using System.Security.Claims;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers;

[Authorize(Roles = "Super Admin")]
[Route("Company")]
public class CompanyController : Controller
{
    private readonly IMasterService _masterService;
    private readonly ICompanyProvisioningService _provisioningService;
    private readonly ICompanySampleDataService _sampleDataService;
    private readonly ILoginHistoryService _loginHistoryService;
    private readonly IAuditService _auditService;
    private readonly Microsoft.AspNetCore.Identity.UserManager<AppUser> _userManager;
    private readonly Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> _roleManager;
    private readonly IWebHostEnvironment _env;

    public CompanyController(
        IMasterService masterService,
        ICompanyProvisioningService provisioningService,
        ICompanySampleDataService sampleDataService,
        ILoginHistoryService loginHistoryService,
        IAuditService auditService,
        Microsoft.AspNetCore.Identity.UserManager<AppUser> userManager,
        Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> roleManager,
        IWebHostEnvironment env)
    {
        _masterService = masterService;
        _provisioningService = provisioningService;
        _sampleDataService = sampleDataService;
        _loginHistoryService = loginHistoryService;
        _auditService = auditService;
        _userManager = userManager;
        _roleManager = roleManager;
        _env = env;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search = null, string? status = null)
    {
        ViewBag.Search = search;
        ViewBag.Status = status ?? "All";
        var companies = await _masterService.GetAllCompaniesAsync(search, status);
        return View(companies);
    }

    [HttpGet("GetCompany/{id}")]
    public async Task<IActionResult> GetCompany(int id)
    {
        var company = await _masterService.GetCompanyByIdAsync(id);
        if (company == null)
            return NotFound(new { message = "Company not found." });

        var companyUsers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _userManager.Users.Where(u => u.CompanyId == id).OrderBy(u => u.CreatedAt)
        );

        AppUser? adminUser = null;
        var userDtos = new List<object>();

        foreach (var u in companyUsers)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var roleStr = string.Join(", ", roles);
            var isAdmin = roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                                         r.Equals("CompanyAdmin", StringComparison.OrdinalIgnoreCase) ||
                                         r.Equals("Company Admin", StringComparison.OrdinalIgnoreCase));
            if (adminUser == null && isAdmin)
            {
                adminUser = u;
            }

            userDtos.Add(new
            {
                id = u.Id,
                userName = u.UserName ?? "—",
                fullName = u.FullName ?? "—",
                email = u.Email ?? "—",
                mobile = u.Mobile ?? "—",
                roles = string.IsNullOrWhiteSpace(roleStr) ? "CompanyUser" : roleStr,
                isActive = u.IsActive,
                createdAt = u.CreatedAt.ToString("dd-MMM-yyyy hh:mm tt")
            });
        }

        if (adminUser == null && companyUsers.Any())
        {
            adminUser = companyUsers.First();
        }

        return Json(new
        {
            company.Id,
            company.CompanyCode,
            company.CompanyName,
            company.BusinessType,
            company.Address,
            company.City,
            company.State,
            company.Country,
            company.Pincode,
            company.Phone,
            company.AlternatePhone,
            company.Email,
            company.Website,
            company.GSTNumber,
            company.PANNumber,
            company.Logo,
            company.Currency,
            company.FinancialYear,
            company.IsActive,
            CreatedAt = company.CreatedAt.ToString("dd-MMM-yyyy hh:mm tt"),
            company.CreatedBy,
            UpdatedAt = company.UpdatedAt?.ToString("dd-MMM-yyyy hh:mm tt") ?? "—",
            company.UpdatedBy,

            // Admin Details
            adminUserId = adminUser?.Id ?? "",
            adminUsername = adminUser?.UserName ?? "",
            adminFullName = adminUser?.FullName ?? "",
            adminEmail = adminUser?.Email ?? "",
            adminMobile = adminUser?.Mobile ?? "",
            adminRole = adminUser != null ? string.Join(", ", await _userManager.GetRolesAsync(adminUser)) : "",
            adminIsActive = adminUser?.IsActive ?? true,

            // All Users List
            users = userDtos
        });
    }

    [HttpGet("CheckCode")]
    public async Task<IActionResult> CheckCode(string code, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Json(new { isAvailable = false, message = "Company code cannot be empty." });

        var isAvailable = await _masterService.IsCompanyCodeAvailableAsync(code, excludeId);
        return Json(new
        {
            isAvailable,
            message = isAvailable
                ? "Company Code is available."
                : "Company Code already exists. Please use a different Company Code."
        });
    }

    [HttpGet("GetProvisioningDefaults")]
    public async Task<IActionResult> GetProvisioningDefaults(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Json(new { adminUsername = "", userUsername = "" });

        var adminUsername = _provisioningService.GenerateAdminUsername(code);
        var userUsername = await _provisioningService.GetNextAvailableUserNumberAsync(code);

        return Json(new { adminUsername, userUsername });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CompanyProvisioningViewModel model)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!ModelState.IsValid)
        {
            var errorList = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            var combinedError = string.Join("; ", errorList);

            if (isAjax)
            {
                return Json(new { success = false, message = combinedError });
            }

            TempData["Error"] = combinedError;
            return RedirectToAction(nameof(Index));
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Super Admin";
        var result = await _provisioningService.ProvisionCompanyAsync(model, _env, currentUserId);

        if (!result.Success)
        {
            if (isAjax)
            {
                return Json(new { success = false, message = result.ErrorMessage ?? "Failed to provision company." });
            }

            TempData["Error"] = result.ErrorMessage ?? "Failed to provision company.";
            return RedirectToAction(nameof(Index));
        }

        if (isAjax)
        {
            return Json(new
            {
                success = true,
                companyId = result.CompanyId,
                companyCode = result.CompanyCode,
                companyName = result.CompanyName,
                adminUsername = result.AdminUsername,
                adminFullName = result.AdminFullName,
                adminRole = result.AdminRole,
                initialUsername = result.InitialUsername,
                initialUserFullName = result.InitialUserFullName,
                initialUserRole = result.InitialUserRole,
                isActive = result.IsActive,
                message = $"Company '{result.CompanyName}' ({result.CompanyCode}) provisioned successfully."
            });
        }

        TempData["ProvisionSuccessJson"] = System.Text.Json.JsonSerializer.Serialize(result);
        TempData["Success"] = $"Company '{result.CompanyName}' ({result.CompanyCode}) and Administrator '{result.AdminUsername}' provisioned successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CompanyEditViewModel model, IFormFile? logoFile)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        if (model.Id <= 0)
        {
            if (isAjax) return Json(ApiResponse.Fail("Invalid company identifier."));
            TempData["Error"] = "Invalid company identifier.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(model.CompanyName))
        {
            if (isAjax) return Json(ApiResponse.Fail("Company Name is required."));
            TempData["Error"] = "Company Name is required.";
            return RedirectToAction(nameof(Index));
        }

        var company = new Company
        {
            Id = model.Id,
            CompanyName = model.CompanyName,
            CompanyCode = model.CompanyCode ?? string.Empty,
            BusinessType = model.BusinessType,
            Address = model.Address,
            City = model.City,
            State = model.State,
            Country = model.Country ?? "India",
            Pincode = model.Pincode,
            Phone = model.Phone,
            AlternatePhone = model.AlternatePhone,
            Email = model.Email,
            Website = model.Website,
            GSTNumber = model.GSTNumber,
            PANNumber = model.PANNumber,
            Currency = model.Currency ?? "INR",
            FinancialYear = model.FinancialYear,
            IsActive = model.IsActive
        };

        var currentUserId = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Super Admin";
        var (success, errorMessage, updatedCompany) = await _masterService.UpdateCompanyAsync(company, logoFile, _env, currentUserId);

        if (!success)
        {
            if (isAjax) return Json(ApiResponse.Fail(errorMessage ?? "Failed to update company."));
            TempData["Error"] = errorMessage ?? "Failed to update company.";
            return RedirectToAction(nameof(Index));
        }

        // Update Administrator Details if provided
        if (!string.IsNullOrWhiteSpace(model.AdminUserId) || !string.IsNullOrWhiteSpace(model.AdminUsername))
        {
            AppUser? adminUser = null;
            if (!string.IsNullOrWhiteSpace(model.AdminUserId))
            {
                adminUser = await _userManager.FindByIdAsync(model.AdminUserId);
            }
            if (adminUser == null && !string.IsNullOrWhiteSpace(model.AdminUsername))
            {
                adminUser = await _userManager.FindByNameAsync(model.AdminUsername);
            }
            if (adminUser == null)
            {
                adminUser = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    _userManager.Users.Where(u => u.CompanyId == model.Id)
                );
            }

            if (adminUser != null)
            {
                if (!string.IsNullOrWhiteSpace(model.AdminFullName))
                    adminUser.FullName = model.AdminFullName.Trim();

                if (!string.IsNullOrWhiteSpace(model.AdminEmail))
                    adminUser.Email = model.AdminEmail.Trim();

                if (model.AdminMobile != null)
                    adminUser.Mobile = model.AdminMobile.Trim();

                adminUser.IsActive = model.IsActive;

                // Handle Password Reset if requested
                if (!string.IsNullOrWhiteSpace(model.AdminNewPassword))
                {
                    if (model.AdminNewPassword != model.AdminConfirmPassword)
                    {
                        if (isAjax) return Json(ApiResponse.Fail("Admin passwords do not match."));
                        TempData["Error"] = "Admin passwords do not match.";
                        return RedirectToAction(nameof(Index));
                    }

                    var token = await _userManager.GeneratePasswordResetTokenAsync(adminUser);
                    var resetResult = await _userManager.ResetPasswordAsync(adminUser, token, model.AdminNewPassword);
                    if (!resetResult.Succeeded)
                    {
                        var pwdError = "Failed to update admin password: " + string.Join("; ", resetResult.Errors.Select(e => e.Description));
                        if (isAjax) return Json(ApiResponse.Fail(pwdError));
                        TempData["Error"] = pwdError;
                        return RedirectToAction(nameof(Index));
                    }

                    adminUser.ClearTextPassword = model.AdminNewPassword;
                }

                await _userManager.UpdateAsync(adminUser);
            }
        }

        if (isAjax) return Json(ApiResponse.Ok($"Company '{updatedCompany?.CompanyName}' details updated successfully.", updatedCompany));
        TempData["Success"] = $"Company '{updatedCompany?.CompanyName}' details updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Super Admin";
        var (success, errorMessage, newStatus) = await _masterService.ToggleCompanyStatusAsync(id, currentUserId);

        if (!success)
        {
            return Json(new { success = false, message = errorMessage ?? "Failed to update status." });
        }

        return Json(new
        {
            success = true,
            isActive = newStatus,
            message = newStatus ? "Company activated successfully." : "Company deactivated successfully."
        });
    }

    [HttpPost("SwitchCompany")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchCompany(int? companyId, string? returnUrl = null)
    {
        if (!User.IsInRole("Super Admin"))
        {
            return Forbid();
        }

        returnUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Content("~/");

        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        var prevCompanyId = HttpContext.Session.GetInt32("ActiveCompanyId");
        var prevCompanyCode = HttpContext.Session.GetString("ActiveCompanyCode");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Super Admin";
        var username = User.Identity?.Name ?? "Super Admin";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Clear active company context -> revert to System View (All Companies)
        if (!companyId.HasValue || companyId.Value <= 0)
        {
            HttpContext.Session.Remove("ActiveCompanyId");
            HttpContext.Session.Remove("ActiveCompanyCode");
            HttpContext.Session.Remove("ActiveCompanyName");

            await _loginHistoryService.RecordCompanySwitchAsync(userId, username, prevCompanyId, prevCompanyCode, null, null, ip);
            await _auditService.LogAsync(
                action: "COMPANY_SWITCH",
                module: "Company",
                description: $"Switched context from '{(prevCompanyCode ?? "System")}' to 'System Mode (All Companies)'",
                oldValues: prevCompanyCode,
                newValues: "System",
                severity: "Info");

            if (isAjax)
            {
                return Json(new { success = true, companyId = (int?)null, message = "Company context cleared. Reverted to System Mode (All Companies)." });
            }

            TempData["Success"] = "Active company context cleared. Now viewing in System Mode (All Companies).";
            return LocalRedirect(returnUrl);
        }

        // Validate that target company exists and is active
        var company = await _masterService.GetCompanyByIdAsync(companyId.Value);
        if (company == null)
        {
            if (isAjax)
            {
                return Json(new { success = false, message = "Selected company does not exist." });
            }
            TempData["Error"] = "Selected company does not exist.";
            return LocalRedirect(returnUrl);
        }

        if (!company.IsActive)
        {
            if (isAjax)
            {
                return Json(new { success = false, message = $"Cannot switch to inactive company '{company.CompanyName}' ({company.CompanyCode}). Please activate it first." });
            }
            TempData["Error"] = $"Cannot switch to inactive company '{company.CompanyName}' ({company.CompanyCode}). Please activate it first.";
            return LocalRedirect(returnUrl);
        }

        // Set active company context in session
        HttpContext.Session.SetInt32("ActiveCompanyId", company.Id);
        HttpContext.Session.SetString("ActiveCompanyCode", company.CompanyCode);
        HttpContext.Session.SetString("ActiveCompanyName", company.CompanyName);

        await _loginHistoryService.RecordCompanySwitchAsync(userId, username, prevCompanyId, prevCompanyCode, company.Id, company.CompanyCode, ip);
        await _auditService.LogAsync(
            action: "COMPANY_SWITCH",
            module: "Company",
            entityName: "Company",
            entityId: company.Id.ToString(),
            description: $"Switched active company context from '{(prevCompanyCode ?? "System")}' to '{company.CompanyCode} - {company.CompanyName}'",
            oldValues: prevCompanyCode ?? "System",
            newValues: company.CompanyCode,
            severity: "Info",
            companyId: company.Id);

        if (isAjax)
        {
            return Json(new
            {
                success = true,
                companyId = company.Id,
                companyCode = company.CompanyCode,
                companyName = company.CompanyName,
                message = $"Active company switched to: {company.CompanyCode} - {company.CompanyName}"
            });
        }

        TempData["Success"] = $"Active company context switched to: {company.CompanyCode} - {company.CompanyName}";
        return LocalRedirect(returnUrl);
    }

    [HttpPost("InitializeSampleData/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InitializeSampleData(int id)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (id <= 0)
        {
            if (isAjax) return Json(new { success = false, message = "Invalid company identifier." });
            TempData["Error"] = "Invalid company identifier.";
            return RedirectToAction(nameof(Index));
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Super Admin";
        var result = await _sampleDataService.InitializeSampleDataAsync(id, currentUserId);

        if (isAjax)
        {
            return Json(new
            {
                success = result.Success,
                message = result.Message ?? (result.Success ? "Sample data initialized successfully." : "Failed to initialize sample data.")
            });
        }

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
