using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize(Roles = "Super Admin,Admin")]
[Route("Settings")]
public class SettingsController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMasterService _masterService;
    private readonly IWebHostEnvironment _env;
    private readonly AppDbContext _context;

    public SettingsController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IMasterService masterService, IWebHostEnvironment env, AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _masterService = masterService;
        _env = env;
        _context = context;
    }

    // --- User Management ---
    [HttpGet("Users")]
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.ToListAsync();
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).Select(r => r.Name!).ToListAsync();
        var userRoles = new Dictionary<string, IList<string>>();

        foreach (var user in users)
        {
            userRoles[user.Id] = await _userManager.GetRolesAsync(user);
        }

        return View(new UsersPageViewModel
        {
            Users = users,
            Roles = roles,
            UserRoles = userRoles
        });
    }

    [HttpPost("SaveUser")]
    public async Task<IActionResult> SaveUser(string email, string fullName, string mobile, string password, string role, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(role) || !await _roleManager.RoleExistsAsync(role))
        {
            TempData["Error"] = "Please select a valid role.";
            return RedirectToAction(nameof(Users));
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing == null)
        {
            var user = new AppUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                Mobile = mobile,
                IsActive = isActive,
                EmailConfirmed = true,
                ClearTextPassword = password
            };
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                TempData["Success"] = "User created successfully.";
            }
            else
            {
                TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
            }
        }
        else
        {
            existing.FullName = fullName;
            existing.Mobile = mobile;
            existing.IsActive = isActive;

            if (!string.IsNullOrWhiteSpace(password))
            {
                existing.ClearTextPassword = password;
                var token = await _userManager.GeneratePasswordResetTokenAsync(existing);
                var resetResult = await _userManager.ResetPasswordAsync(existing, token, password);
                if (!resetResult.Succeeded)
                {
                    TempData["Error"] = "Failed to update password: " + string.Join("; ", resetResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Users));
                }
            }

            var result = await _userManager.UpdateAsync(existing);
            if (result.Succeeded)
            {
                var currentRoles = await _userManager.GetRolesAsync(existing);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(existing, currentRoles);
                }

                await _userManager.AddToRoleAsync(existing, role);
                TempData["Success"] = "User updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update user.";
            }
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost("DeleteUser")]
    public async Task<IActionResult> DeleteUser(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            if (loggedInUser != null && loggedInUser.Email == email)
            {
                return Json(new { success = false, message = "You cannot delete your own logged-in account." });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true });
            }
            return Json(new { success = false, message = string.Join("; ", result.Errors.Select(e => e.Description)) });
        }
        return Json(new { success = false, message = "User not found." });
    }

    // --- Role Configuration ---
    [HttpGet("Roles")]
    public async Task<IActionResult> Roles()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        return View(roles);
    }

    [HttpPost("SaveRole")]
    public async Task<IActionResult> SaveRole(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (!await _roleManager.RoleExistsAsync(name))
            {
                await _roleManager.CreateAsync(new IdentityRole(name));
                TempData["Success"] = "Role added successfully.";
            }
            else
            {
                TempData["Error"] = "Role already exists.";
            }
        }
        return RedirectToAction(nameof(Roles));
    }

    // --- Company Setup ---
    [HttpGet("CompanySettings")]
    public async Task<IActionResult> CompanySettings()
    {
        var company = await _masterService.GetCompanyAsync() ?? new Company();
        return View(company);
    }

    [HttpPost("CompanySettings")]
    public async Task<IActionResult> SaveCompanySettings(Company company, IFormFile? logoFile, bool resetSalesCounter = false, bool resetPurchaseCounter = false)
    {
        if (resetSalesCounter)
        {
            company.SalesBillNextNumber = company.SalesBillStartNumber;
        }

        if (resetPurchaseCounter)
        {
            company.PurchaseBillNextNumber = company.PurchaseBillStartNumber;
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _masterService.SaveCompanyWithLogoAsync(company, logoFile, _env);
                TempData["Success"] = "Company settings saved successfully.";
                return RedirectToAction(nameof(CompanySettings));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
        }
        else
        {
            TempData["Error"] = "Please correct the highlighted fields.";
        }

        return View("CompanySettings", company);
    }

    [HttpGet("InvoiceSettings")]
    public async Task<IActionResult> InvoiceSettings()
    {
        var company = await _masterService.GetCompanyAsync() ?? new Company();
        return View(company);
    }

    // --- System Config ---
    [HttpGet("SystemConfig")]
    public IActionResult SystemConfig()
    {
        return View();
    }

    // --- Dynamic User Permissions ---
    private async Task<List<ScreenPermission>> GetOrInitializeUserPermissionsAsync(AppUser user)
    {
        var permissions = await _context.ScreenPermissions
            .Where(sp => sp.IsActive && sp.UserId == user.Id)
            .ToListAsync();

        if (permissions.Count < 47)
        {
            if (permissions.Any())
            {
                _context.ScreenPermissions.RemoveRange(permissions);
                await _context.SaveChangesAsync();
                permissions.Clear();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? "Sales User";
            
            var pageMappings = new Dictionary<string, string[]>
            {
                { "Masters", new[] { "Company Master", "Customer Master", "Supplier Master", "Product Master", "Category Master", "Brand Master", "Unit Master", "Warehouse Master", "Ledger Master", "Employee Master", "Account Groups", "Bank Master", "Tax Settings", "Payment Modes" } },
                { "Sales", new[] { "Quotation", "Sales Order", "Delivery Challan", "Sales Invoice", "Sales Return" } },
                { "Purchase", new[] { "Purchase Order", "Goods Receipt Note", "Purchase Invoice", "Purchase Return" } },
                { "Inventory", new[] { "Stock Opening", "Stock Transfer", "Stock Adjustment", "Physical Stock" } },
                { "Accounts", new[] { "Receipt Voucher", "Payment Voucher", "Contra Voucher", "Journal Voucher", "Debit Note", "Credit Note", "Cash Book", "Bank Book" } },
                { "CRM", new[] { "Leads", "Follow Ups", "Opportunities", "Pipeline View" } },
                { "Reports", new[] { "Sales Reports", "Purchase Reports", "Inventory Reports", "Accounting Reports" } },
                { "Settings", new[] { "User Management", "Role Configuration", "Company Setup", "System Settings" } }
            };

            foreach (var module in pageMappings)
            {
                var moduleName = module.Key;
                foreach (var screen in module.Value)
                {
                    var permission = new ScreenPermission
                    {
                        UserId = user.Id,
                        ScreenName = screen,
                        CanView = false,
                        CanEdit = false,
                        CanDelete = false
                    };

                    // Define defaults based on roles and screen categories
                    if (roleName is "Super Admin" or "Admin")
                    {
                        permission.CanView = true;
                        permission.CanEdit = true;
                        permission.CanDelete = true;
                    }
                    else if (roleName == "Manager")
                    {
                        if (moduleName != "Settings")
                        {
                            permission.CanView = true;
                            permission.CanEdit = true;
                            permission.CanDelete = true;
                        }
                    }
                    else if (roleName == "Sales User")
                    {
                        if (moduleName == "Sales" || moduleName == "CRM")
                        {
                            permission.CanView = true;
                            permission.CanEdit = true;
                            permission.CanDelete = false;
                        }
                        else if (moduleName == "Inventory")
                        {
                            permission.CanView = true;
                        }
                    }
                    else if (roleName == "Purchase User")
                    {
                        if (moduleName == "Purchase" || moduleName == "Inventory")
                        {
                            permission.CanView = true;
                            permission.CanEdit = true;
                            permission.CanDelete = false;
                        }
                    }
                    else if (roleName == "Accountant")
                    {
                        if (moduleName == "Accounts")
                        {
                            permission.CanView = true;
                            permission.CanEdit = true;
                            permission.CanDelete = false;
                        }
                        else if (moduleName == "Reports" && screen == "Accounting Reports")
                        {
                            permission.CanView = true;
                        }
                    }

                    _context.ScreenPermissions.Add(permission);
                    permissions.Add(permission);
                }
            }

            await _context.SaveChangesAsync();
        }

        return permissions;
    }

    [HttpGet("Permissions")]
    public async Task<IActionResult> Permissions(string? userId = null)
    {
        var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
        
        if (string.IsNullOrEmpty(userId) && users.Any())
        {
            userId = users.First().Id;
        }

        var permissions = new List<ScreenPermission>();
        if (!string.IsNullOrEmpty(userId))
        {
            var selectedUser = users.FirstOrDefault(u => u.Id == userId);
            if (selectedUser != null)
            {
                permissions = await GetOrInitializeUserPermissionsAsync(selectedUser);
            }
        }

        return View(new UserPermissionsPageViewModel
        {
            SelectedUserId = userId ?? string.Empty,
            Users = users,
            Permissions = permissions
        });
    }

    [HttpPost("SavePermissions")]
    public async Task<IActionResult> SavePermissions(string userId, List<ScreenPermissionDto> permissions)
    {
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "Invalid user specified.";
            return RedirectToAction(nameof(Permissions));
        }

        var existingPermissions = await _context.ScreenPermissions
            .Where(sp => sp.IsActive && sp.UserId == userId)
            .ToListAsync();

        foreach (var pDto in permissions)
        {
            var dbPerm = existingPermissions.FirstOrDefault(x => x.ScreenName == pDto.ScreenName);
            if (dbPerm != null)
            {
                dbPerm.CanView = pDto.CanView;
                dbPerm.CanEdit = pDto.CanEdit;
                dbPerm.CanDelete = pDto.CanDelete;
                dbPerm.UpdatedAt = DateTime.Now;
            }
            else
            {
                _context.ScreenPermissions.Add(new ScreenPermission
                {
                    UserId = userId,
                    ScreenName = pDto.ScreenName,
                    CanView = pDto.CanView,
                    CanEdit = pDto.CanEdit,
                    CanDelete = pDto.CanDelete
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "User permissions updated successfully.";
        return RedirectToAction(nameof(Permissions), new { userId = userId });
    }

    public class ScreenPermissionDto
    {
        public string ScreenName { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
