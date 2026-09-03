using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Services;

public class CompanyProvisioningService : ICompanyProvisioningService
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMasterService _masterService;
    private readonly ICompanySampleDataService _sampleDataService;

    public CompanyProvisioningService(
        AppDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IMasterService masterService,
        ICompanySampleDataService sampleDataService)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _masterService = masterService;
        _sampleDataService = sampleDataService;
    }

    public string GenerateAdminUsername(string companyCode)
    {
        var normalized = companyCode?.Trim().ToUpperInvariant() ?? "COMPANY";
        return $"{normalized}_ADMIN";
    }

    public async Task<string> GetNextAvailableUserNumberAsync(string companyCode)
    {
        var normalized = companyCode?.Trim().ToUpperInvariant() ?? "COMPANY";
        var prefix = $"{normalized}_USER";

        var existingUsernames = await _userManager.Users
            .Where(u => u.UserName != null && u.UserName.StartsWith(prefix))
            .Select(u => u.UserName!)
            .ToListAsync();

        var maxNum = 0;
        var regex = new Regex($@"^{Regex.Escape(prefix)}(\d+)$", RegexOptions.IgnoreCase);

        foreach (var username in existingUsernames)
        {
            var match = regex.Match(username);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var num))
            {
                if (num > maxNum)
                {
                    maxNum = num;
                }
            }
        }

        var nextNum = maxNum + 1;
        return $"{prefix}{nextNum:D2}";
    }

    public async Task<bool> IsUsernameAvailableAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        var existing = await _userManager.FindByNameAsync(username.Trim());
        return existing == null;
    }

    public async Task<CompanyProvisioningResult> ProvisionCompanyAsync(CompanyProvisioningViewModel model, IWebHostEnvironment env, string currentUserId)
    {
        var result = new CompanyProvisioningResult();

        // 1. Validate Company Inputs
        if (string.IsNullOrWhiteSpace(model.CompanyName))
        {
            result.Success = false;
            result.ErrorMessage = "Company Name is required.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(model.CompanyCode))
        {
            result.Success = false;
            result.ErrorMessage = "Company Code is required.";
            return result;
        }

        var normalizedCode = model.CompanyCode.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(normalizedCode, @"^[A-Z0-9\-_]{2,20}$"))
        {
            result.Success = false;
            result.ErrorMessage = "Company Code must be 2 to 20 alphanumeric characters (hyphens and underscores allowed).";
            return result;
        }

        // Check company code availability across all tenants
        var isCodeAvailable = await _masterService.IsCompanyCodeAvailableAsync(normalizedCode);
        if (!isCodeAvailable)
        {
            result.Success = false;
            result.ErrorMessage = $"Company Code '{normalizedCode}' already exists. Please use a different Company Code.";
            return result;
        }

        // 2. Validate Company Admin Details
        if (string.IsNullOrWhiteSpace(model.AdminFullName))
        {
            result.Success = false;
            result.ErrorMessage = "Company Admin Name is required.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(model.AdminPassword))
        {
            result.Success = false;
            result.ErrorMessage = "Company Admin Password is required.";
            return result;
        }

        if (model.AdminPassword != model.AdminConfirmPassword)
        {
            result.Success = false;
            result.ErrorMessage = "Admin password and confirm password do not match.";
            return result;
        }

        var adminUsername = string.IsNullOrWhiteSpace(model.AdminUsername)
            ? GenerateAdminUsername(normalizedCode)
            : model.AdminUsername.Trim().ToUpperInvariant();

        if (!await IsUsernameAvailableAsync(adminUsername))
        {
            result.Success = false;
            result.ErrorMessage = $"Admin username '{adminUsername}' already exists in the system. Please use a unique company code.";
            return result;
        }

        // 3. Validate Initial User Details (if enabled)
        string? initialUsername = null;
        if (model.CreateInitialUser)
        {
            if (string.IsNullOrWhiteSpace(model.UserPassword))
            {
                result.Success = false;
                result.ErrorMessage = "Initial User Password is required when creating an initial user.";
                return result;
            }

            if (model.UserPassword != model.UserConfirmPassword)
            {
                result.Success = false;
                result.ErrorMessage = "Initial user password and confirm password do not match.";
                return result;
            }

            initialUsername = string.IsNullOrWhiteSpace(model.UserUsername)
                ? await GetNextAvailableUserNumberAsync(normalizedCode)
                : model.UserUsername.Trim().ToUpperInvariant();

            if (!await IsUsernameAvailableAsync(initialUsername))
            {
                result.Success = false;
                result.ErrorMessage = $"Initial username '{initialUsername}' already exists. Please pick a different username.";
                return result;
            }
        }

        // 4. Ensure Required Roles Exist
        string[] requiredRoles = { "Super Admin", "Admin", "CompanyAdmin", "CompanyUser", "Sales User", "Purchase User", "Accountant", "Manager" };
        foreach (var r in requiredRoles)
        {
            if (!await _roleManager.RoleExistsAsync(r))
            {
                await _roleManager.CreateAsync(new IdentityRole(r));
            }
        }

        // 5. Execute Atomic Database Transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // A. Create Company Entity
            var company = new Company
            {
                CompanyName = model.CompanyName.Trim(),
                CompanyCode = normalizedCode,
                BusinessType = model.BusinessType?.Trim(),
                Address = model.Address?.Trim(),
                City = model.City?.Trim(),
                State = model.State?.Trim(),
                Country = string.IsNullOrWhiteSpace(model.Country) ? "India" : model.Country.Trim(),
                Pincode = model.Pincode?.Trim(),
                Phone = model.Phone?.Trim(),
                AlternatePhone = model.AlternatePhone?.Trim(),
                Email = model.Email?.Trim(),
                Website = model.Website?.Trim(),
                GSTNumber = model.GSTNumber?.Trim().ToUpperInvariant(),
                PANNumber = model.PANNumber?.Trim().ToUpperInvariant(),
                Currency = string.IsNullOrWhiteSpace(model.Currency) ? "INR" : model.Currency.Trim(),
                FinancialYear = string.IsNullOrWhiteSpace(model.FinancialYear)
                    ? $"{DateTime.Now.Year}-{(DateTime.Now.Year + 1)}"
                    : model.FinancialYear.Trim(),
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now,
                CreatedBy = currentUserId
            };

            // Handle Logo file if provided
            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "company");
                Directory.CreateDirectory(uploadsDir);

                var extension = Path.GetExtension(model.LogoFile.FileName).ToLowerInvariant();
                if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp"))
                {
                    await transaction.RollbackAsync();
                    result.Success = false;
                    result.ErrorMessage = "Logo must be PNG, JPG, JPEG, or WEBP.";
                    return result;
                }

                if (model.LogoFile.Length > 2 * 1024 * 1024)
                {
                    await transaction.RollbackAsync();
                    result.Success = false;
                    result.ErrorMessage = "Logo file size must be under 2 MB.";
                    return result;
                }

                var fileName = $"logo_{normalizedCode}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.LogoFile.CopyToAsync(stream);
                }

                company.Logo = $"/uploads/company/{fileName}";
            }

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            var companyId = company.Id;

            // B. Create Company Admin User
            var adminUser = new AppUser
            {
                UserName = adminUsername,
                Email = string.IsNullOrWhiteSpace(model.AdminEmail) ? $"{adminUsername.ToLowerInvariant()}@tenant.local" : model.AdminEmail.Trim(),
                FullName = model.AdminFullName.Trim(),
                Mobile = model.AdminMobile?.Trim(),
                EmailConfirmed = true,
                ClearTextPassword = model.AdminPassword,
                IsActive = company.IsActive,
                CompanyId = companyId,
                CreatedAt = DateTime.Now
            };

            var adminCreateResult = await _userManager.CreateAsync(adminUser, model.AdminPassword);
            if (!adminCreateResult.Succeeded)
            {
                await transaction.RollbackAsync();
                result.Success = false;
                result.ErrorMessage = "Failed to create Company Admin: " + string.Join("; ", adminCreateResult.Errors.Select(e => e.Description));
                return result;
            }

            // Assign CompanyAdmin and Admin roles
            await _userManager.AddToRoleAsync(adminUser, "CompanyAdmin");
            await _userManager.AddToRoleAsync(adminUser, "Admin");

            // C. Create Initial User (if selected)
            if (model.CreateInitialUser && !string.IsNullOrEmpty(initialUsername))
            {
                var initialUser = new AppUser
                {
                    UserName = initialUsername,
                    Email = string.IsNullOrWhiteSpace(model.UserEmail) ? $"{initialUsername.ToLowerInvariant()}@tenant.local" : model.UserEmail.Trim(),
                    FullName = string.IsNullOrWhiteSpace(model.UserFullName) ? $"{normalizedCode} User 01" : model.UserFullName.Trim(),
                    Mobile = model.UserMobile?.Trim(),
                    EmailConfirmed = true,
                    ClearTextPassword = model.UserPassword,
                    IsActive = company.IsActive,
                    CompanyId = companyId,
                    CreatedAt = DateTime.Now
                };

                var userCreateResult = await _userManager.CreateAsync(initialUser, model.UserPassword!);
                if (!userCreateResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    result.Success = false;
                    result.ErrorMessage = "Failed to create Initial Company User: " + string.Join("; ", userCreateResult.Errors.Select(e => e.Description));
                    return result;
                }

                var assignedRole = string.IsNullOrWhiteSpace(model.UserRole) ? "CompanyUser" : model.UserRole;
                if (!await _roleManager.RoleExistsAsync(assignedRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(assignedRole));
                }

                await _userManager.AddToRoleAsync(initialUser, assignedRole);
                if (assignedRole != "CompanyUser")
                {
                    await _userManager.AddToRoleAsync(initialUser, "CompanyUser");
                }

                result.InitialUsername = initialUsername;
                result.InitialUserFullName = initialUser.FullName;
                result.InitialUserRole = assignedRole;
            }

            // D. Initialize Sample / Test Master Data (if selected)
            if (model.CreateSampleData)
            {
                var sampleInitResult = await _sampleDataService.InitializeSampleDataAsync(companyId, currentUserId);
                if (!sampleInitResult.Success)
                {
                    await transaction.RollbackAsync();
                    result.Success = false;
                    result.ErrorMessage = "Failed to initialize sample data: " + sampleInitResult.Message;
                    return result;
                }

                result.SampleDataInitialized = true;
                result.SampleDataSummary = sampleInitResult.GetSummary();
            }

            // E. Commit Transaction
            await transaction.CommitAsync();

            result.Success = true;
            result.CompanyId = companyId;
            result.CompanyCode = normalizedCode;
            result.CompanyName = company.CompanyName;
            result.AdminUsername = adminUsername;
            result.AdminFullName = adminUser.FullName;
            result.AdminRole = "CompanyAdmin";
            result.IsActive = company.IsActive;

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Success = false;
            result.ErrorMessage = "An unexpected error occurred during company provisioning: " + ex.Message;
            return result;
        }
    }
}
