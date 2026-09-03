using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize(Roles = "Super Admin,Company Admin,CompanyAdmin,Admin")]
[Route("Settings")]
public class SettingsController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMasterService _masterService;
    private readonly IWebHostEnvironment _env;
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;
    private readonly ILoginHistoryService _loginHistoryService;

    public SettingsController(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IMasterService masterService,
        IWebHostEnvironment env,
        AppDbContext context,
        ICompanyContext companyContext,
        ILoginHistoryService loginHistoryService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _masterService = masterService;
        _env = env;
        _context = context;
        _companyContext = companyContext;
        _loginHistoryService = loginHistoryService;
    }

    // --- User Management ---
    [HttpGet("Users")]
    public async Task<IActionResult> Users(int? companyFilter = null, string? roleFilter = null, string? statusFilter = null, string? search = null)
    {
        var isSuperAdmin = User.IsInRole("Super Admin");
        var activeCompanyId = _companyContext.CurrentCompanyId;

        IQueryable<AppUser> query = _userManager.Users.Include(u => u.Company);

        if (!isSuperAdmin)
        {
            // Company Admin is strictly restricted to their active company
            if (!activeCompanyId.HasValue)
            {
                TempData["Error"] = "No active company context assigned.";
                return View(new UsersPageViewModel());
            }
            query = query.Where(u => u.CompanyId == activeCompanyId.Value);
        }
        else
        {
            // Super Admin can filter by company
            if (companyFilter.HasValue && companyFilter.Value > 0)
            {
                query = query.Where(u => u.CompanyId == companyFilter.Value);
            }
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            if (statusFilter.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => u.IsActive);
            }
            else if (statusFilter.Equals("inactive", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => !u.IsActive);
            }
        }

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u => (u.FullName != null && u.FullName.ToLower().Contains(s)) ||
                                     (u.Email != null && u.Email.ToLower().Contains(s)) ||
                                     (u.Mobile != null && u.Mobile.ToLower().Contains(s)));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

        // Load roles for users
        var userRoles = new Dictionary<string, IList<string>>();
        foreach (var user in users)
        {
            userRoles[user.Id] = await _userManager.GetRolesAsync(user);
        }

        // Filter by role if requested
        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            users = users.Where(u => userRoles.TryGetValue(u.Id, out var rList) && rList.Contains(roleFilter)).ToList();
        }

        // Load active companies for Super Admin dropdown
        var companies = isSuperAdmin
            ? await _context.Companies.IgnoreQueryFilters().Where(c => c.IsActive).OrderBy(c => c.CompanyName).ToListAsync()
            : new List<Company>();

        // Load available roles (Company Admin cannot assign Super Admin)
        var allRoles = await _roleManager.Roles.OrderBy(r => r.Name).Select(r => r.Name!).ToListAsync();
        var assignableRoles = isSuperAdmin
            ? allRoles
            : allRoles.Where(r => r is not ("Super Admin" or "Admin")).ToList();

        // Fetch last login timestamps
        var userIds = users.Select(u => u.Id).ToHashSet();
        var lastLoginsList = await _context.LoginHistories
            .IgnoreQueryFilters()
            .Where(h => h.UserId != null && userIds.Contains(h.UserId) && h.Status == "Success")
            .GroupBy(h => h.UserId!)
            .Select(g => new { UserId = g.Key, LastLogin = g.Max(h => h.LoginTime) })
            .ToListAsync();

        var lastLogins = lastLoginsList.ToDictionary(x => x.UserId, x => (DateTime?)x.LastLogin);

        return View(new UsersPageViewModel
        {
            Users = users,
            Roles = assignableRoles,
            UserRoles = userRoles,
            LastLogins = lastLogins,
            Companies = companies,
            IsSuperAdmin = isSuperAdmin,
            CurrentCompanyId = activeCompanyId,
            SelectedCompanyFilter = companyFilter,
            SelectedRoleFilter = roleFilter,
            SelectedStatusFilter = statusFilter,
            SearchTerm = search
        });
    }

    private bool IsAjaxRequest() =>
        Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
        Request.Headers["Accept"].ToString().Contains("application/json") ||
        Request.ContentType?.Contains("application/json") == true;

    [HttpPost("SaveUser")]
    public async Task<IActionResult> SaveUser(string email, string fullName, string mobile, string password, string? confirmPassword, string role, int? companyId, bool isActive = true)
    {
        var isSuperAdmin = User.IsInRole("Super Admin");
        var activeCompanyId = _companyContext.CurrentCompanyId;
        var currentUserName = User.Identity?.Name ?? "System";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Validate role exists
        if (string.IsNullOrWhiteSpace(role) || !await _roleManager.RoleExistsAsync(role))
        {
            if (IsAjaxRequest()) return Json(ApiResponse.Fail("Please select a valid role."));
            TempData["Error"] = "Please select a valid role.";
            return RedirectToAction(nameof(Users));
        }

        // Non-Super Admin cannot assign Super Admin or Admin roles
        if (!isSuperAdmin && (role is "Super Admin" or "Admin"))
        {
            if (IsAjaxRequest()) return Json(ApiResponse.Fail("Access Denied: You cannot assign system administrative roles."));
            TempData["Error"] = "Access Denied: You cannot assign system administrative roles.";
            return RedirectToAction(nameof(Users));
        }

        // Company resolution
        int? targetCompanyId;
        if (isSuperAdmin)
        {
            targetCompanyId = companyId;
            if (targetCompanyId.HasValue)
            {
                var companyExists = await _context.Companies.IgnoreQueryFilters().AnyAsync(c => c.Id == targetCompanyId.Value);
                if (!companyExists)
                {
                    if (IsAjaxRequest()) return Json(ApiResponse.Fail("Selected company does not exist."));
                    TempData["Error"] = "Selected company does not exist.";
                    return RedirectToAction(nameof(Users));
                }
            }
        }
        else
        {
            // Strictly stamp Company Admin's company ID
            if (!activeCompanyId.HasValue)
            {
                if (IsAjaxRequest()) return Json(ApiResponse.Fail("No active company context assigned."));
                TempData["Error"] = "No active company context assigned.";
                return RedirectToAction(nameof(Users));
            }
            targetCompanyId = activeCompanyId.Value;
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing == null)
        {
            // CREATE NEW USER
            if (string.IsNullOrWhiteSpace(password))
            {
                if (IsAjaxRequest()) return Json(ApiResponse.Fail("Password is required for new users.", new Dictionary<string, string> { { "password", "Password is required" } }));
                TempData["Error"] = "Password is required for new users.";
                return RedirectToAction(nameof(Users));
            }

            if (!string.IsNullOrWhiteSpace(confirmPassword) && password != confirmPassword)
            {
                if (IsAjaxRequest()) return Json(ApiResponse.Fail("Passwords do not match.", new Dictionary<string, string> { { "confirmPassword", "Passwords do not match" } }));
                TempData["Error"] = "Passwords do not match.";
                return RedirectToAction(nameof(Users));
            }

            var user = new AppUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                Mobile = mobile,
                IsActive = isActive,
                EmailConfirmed = true,
                ClearTextPassword = password,
                CompanyId = targetCompanyId,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                await GetOrInitializeUserPermissionsAsync(user);

                await _loginHistoryService.RecordUserActivityAsync(
                    user.Id,
                    currentUserName,
                    isSuperAdmin ? "Super Admin" : "Company Admin",
                    "USER_CREATED",
                    $"Created user '{email}' ({fullName}) with role '{role}' for company ID {targetCompanyId}",
                    targetCompanyId,
                    ip);

                if (IsAjaxRequest()) return Json(ApiResponse.Ok("User created successfully.", user));
                TempData["Success"] = "User created successfully.";
            }
            else
            {
                var errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
                if (IsAjaxRequest()) return Json(ApiResponse.Fail(errorMsg));
                TempData["Error"] = errorMsg;
            }
        }
        else
        {
            // UPDATE EXISTING USER
            // Enforce tenant isolation for Company Admin
            if (!isSuperAdmin)
            {
                if (existing.CompanyId != activeCompanyId)
                {
                    await _loginHistoryService.RecordUserActivityAsync(
                        existing.Id,
                        currentUserName,
                        "Company Admin",
                        "SECURITY_VIOLATION",
                        $"Unauthorized attempt to update user '{email}' belonging to company ID {existing.CompanyId}",
                        activeCompanyId,
                        ip);

                    if (IsAjaxRequest()) return Json(ApiResponse.Fail("Access Denied: You do not have permission to modify users of another company."));
                    TempData["Error"] = "Access Denied: You do not have permission to modify users of another company.";
                    return RedirectToAction(nameof(Users));
                }
            }

            existing.FullName = fullName;
            existing.Mobile = mobile;
            existing.IsActive = isActive;

            if (isSuperAdmin && targetCompanyId.HasValue)
            {
                existing.CompanyId = targetCompanyId.Value;
            }

            if (!string.IsNullOrWhiteSpace(password))
            {
                if (!string.IsNullOrWhiteSpace(confirmPassword) && password != confirmPassword)
                {
                    if (IsAjaxRequest()) return Json(ApiResponse.Fail("Passwords do not match.", new Dictionary<string, string> { { "confirmPassword", "Passwords do not match" } }));
                    TempData["Error"] = "Passwords do not match.";
                    return RedirectToAction(nameof(Users));
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(existing);
                var resetResult = await _userManager.ResetPasswordAsync(existing, token, password);
                if (!resetResult.Succeeded)
                {
                    var msg = "Failed to update password: " + string.Join("; ", resetResult.Errors.Select(e => e.Description));
                    if (IsAjaxRequest()) return Json(ApiResponse.Fail(msg));
                    TempData["Error"] = msg;
                    return RedirectToAction(nameof(Users));
                }

                existing.ClearTextPassword = password;
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

                await _loginHistoryService.RecordUserActivityAsync(
                    existing.Id,
                    currentUserName,
                    isSuperAdmin ? "Super Admin" : "Company Admin",
                    "USER_UPDATED",
                    $"Updated user '{email}' ({fullName}), role '{role}'",
                    existing.CompanyId,
                    ip);

                if (IsAjaxRequest()) return Json(ApiResponse.Ok("User updated successfully.", existing));
                TempData["Success"] = "User updated successfully.";
            }
            else
            {
                var msg = "Failed to update user: " + string.Join("; ", result.Errors.Select(e => e.Description));
                if (IsAjaxRequest()) return Json(ApiResponse.Fail(msg));
                TempData["Error"] = msg;
            }
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost("ToggleUserStatus")]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var isSuperAdmin = User.IsInRole("Super Admin");
        var activeCompanyId = _companyContext.CurrentCompanyId;
        var currentUserName = User.Identity?.Name ?? "System";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Json(ApiResponse.Fail("User not found."));
        }

        // Security check for Company Admin
        if (!isSuperAdmin && user.CompanyId != activeCompanyId)
        {
            return Json(ApiResponse.Fail("Access Denied: You cannot modify users belonging to another company."));
        }

        // Prevent toggling own account
        var loggedInUser = await _userManager.GetUserAsync(User);
        if (loggedInUser != null && loggedInUser.Id == user.Id)
        {
            return Json(ApiResponse.Fail("You cannot deactivate your own logged-in account."));
        }

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            var action = user.IsActive ? "USER_ACTIVATED" : "USER_DEACTIVATED";
            await _loginHistoryService.RecordUserActivityAsync(
                user.Id,
                currentUserName,
                isSuperAdmin ? "Super Admin" : "Company Admin",
                action,
                $"{action} for user '{user.Email}'",
                user.CompanyId,
                ip);

            return Json(ApiResponse.Ok(user.IsActive ? "User activated." : "User deactivated.", new { isActive = user.IsActive }));
        }

        return Json(ApiResponse.Fail(string.Join("; ", result.Errors.Select(e => e.Description))));
    }

    [HttpPost("DeleteUser")]
    public async Task<IActionResult> DeleteUser(string email)
    {
        var isSuperAdmin = User.IsInRole("Super Admin");
        var activeCompanyId = _companyContext.CurrentCompanyId;
        var currentUserName = User.Identity?.Name ?? "System";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Json(ApiResponse.Fail("User not found."));
        }

        // Tenant boundary enforcement
        if (!isSuperAdmin && user.CompanyId != activeCompanyId)
        {
            await _loginHistoryService.RecordUserActivityAsync(
                user.Id,
                currentUserName,
                "Company Admin",
                "SECURITY_VIOLATION",
                $"Unauthorized delete attempt for user '{email}' belonging to company ID {user.CompanyId}",
                activeCompanyId,
                ip);

            return Json(ApiResponse.Fail("Access Denied: You cannot delete users belonging to another company."));
        }

        var loggedInUser = await _userManager.GetUserAsync(User);
        if (loggedInUser != null && loggedInUser.Email == email)
        {
            return Json(ApiResponse.Fail("You cannot delete your own logged-in account."));
        }

        if (await _userManager.IsInRoleAsync(user, "Super Admin"))
        {
            return Json(ApiResponse.Fail("Super Admin user cannot be deleted."));
        }

        // Soft-delete to preserve audit trail
        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            await _loginHistoryService.RecordUserActivityAsync(
                user.Id,
                currentUserName,
                isSuperAdmin ? "Super Admin" : "Company Admin",
                "USER_DEACTIVATED",
                $"Deactivated user '{email}'",
                user.CompanyId,
                ip);

            return Json(ApiResponse.Ok("User has been deactivated successfully."));
        }

        return Json(ApiResponse.Fail(string.Join("; ", result.Errors.Select(e => e.Description))));
    }

    // --- Role Configuration ---
    [HttpGet("Roles")]
    public async Task<IActionResult> Roles()
    {
        var isSuperAdmin = User.IsInRole("Super Admin");
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

        var userCounts = new Dictionary<string, int>();
        foreach (var role in roles)
        {
            var count = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
            userCounts[role.Name!] = count;
        }

        return View(new RolesPageViewModel
        {
            Roles = roles,
            UserCounts = userCounts,
            IsSuperAdmin = isSuperAdmin
        });
    }

    [HttpPost("SaveRole")]
    public async Task<IActionResult> SaveRole(string name)
    {
        var isSuperAdmin = User.IsInRole("Super Admin");
        if (!isSuperAdmin)
        {
            if (IsAjaxRequest()) return Json(ApiResponse.Fail("Access Denied: Only Super Admin can manage system security roles."));
            TempData["Error"] = "Access Denied: Only Super Admin can manage system security roles.";
            return RedirectToAction(nameof(Roles));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            if (IsAjaxRequest()) return Json(ApiResponse.Fail("Role name is required."));
            TempData["Error"] = "Role name is required.";
            return RedirectToAction(nameof(Roles));
        }

        name = name.Trim();
        if (await _roleManager.RoleExistsAsync(name))
        {
            if (IsAjaxRequest()) return Json(ApiResponse.Fail($"Role '{name}' already exists."));
            TempData["Error"] = $"Role '{name}' already exists.";
            return RedirectToAction(nameof(Roles));
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(name));
        if (result.Succeeded)
        {
            await _loginHistoryService.RecordUserActivityAsync(
                null,
                User.Identity?.Name ?? "System",
                "Super Admin",
                "ROLE_CREATED",
                $"Created new security role '{name}'");

            if (IsAjaxRequest()) return Json(ApiResponse.Ok($"Role '{name}' created successfully."));
            TempData["Success"] = $"Role '{name}' created successfully.";
        }
        else
        {
            var msg = string.Join("; ", result.Errors.Select(e => e.Description));
            if (IsAjaxRequest()) return Json(ApiResponse.Fail(msg));
            TempData["Error"] = msg;
        }

        return RedirectToAction(nameof(Roles));
    }

    [HttpPost("DeleteRole")]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        var isSuperAdmin = User.IsInRole("Super Admin");
        if (!isSuperAdmin)
        {
            return Json(ApiResponse.Fail("Access Denied: Only Super Admin can delete roles."));
        }

        // Protected system roles
        var protectedRoles = new[] { "Super Admin", "Admin", "Company Admin", "CompanyAdmin", "Manager", "Employee", "Accountant", "Sales", "Purchase", "Viewer" };
        if (protectedRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
        {
            return Json(ApiResponse.Fail($"Role '{roleName}' is a protected system role and cannot be deleted."));
        }

        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return Json(ApiResponse.Fail("Role not found."));
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
        if (usersInRole.Any())
        {
            return Json(ApiResponse.Fail($"Cannot delete role '{roleName}' because {usersInRole.Count} user(s) are currently assigned to it."));
        }

        var result = await _roleManager.DeleteAsync(role);
        if (result.Succeeded)
        {
            await _loginHistoryService.RecordUserActivityAsync(
                null,
                User.Identity?.Name ?? "System",
                "Super Admin",
                "ROLE_DELETED",
                $"Deleted security role '{roleName}'");

            return Json(ApiResponse.Ok($"Role '{roleName}' deleted successfully."));
        }

        return Json(ApiResponse.Fail(string.Join("; ", result.Errors.Select(e => e.Description))));
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

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest()) return Json(ApiResponse.Fail("Please correct the highlighted fields.", ModelState.Where(x => x.Value?.Errors.Count > 0).ToDictionary(k => k.Key, v => v.Value!.Errors.First().ErrorMessage)));
            TempData["Error"] = "Please correct the highlighted fields.";
            return View("CompanySettings", company);
        }

        try
        {
            await _masterService.SaveCompanyWithLogoAsync(company, logoFile, _env);
            if (IsAjaxRequest()) return Json(ApiResponse.Ok("Company settings saved successfully.", company));
            TempData["Success"] = "Company settings saved successfully.";
            return RedirectToAction(nameof(CompanySettings));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest()) return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return View("CompanySettings", company);
        }
    }

    [HttpGet("InvoiceSettings")]
    public async Task<IActionResult> InvoiceSettings()
    {
        var company = await _masterService.GetCompanyAsync() ?? new Company();
        return View(company);
    }

    // --- System Config ---
    [Authorize(Roles = "Super Admin,Admin")]
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
                        CanDelete = false,
                        CompanyId = user.CompanyId
                    };

                    if (roleName is "Super Admin" or "Admin")
                    {
                        permission.CanView = true;
                        permission.CanEdit = true;
                        permission.CanDelete = true;
                    }
                    else if (roleName is "Company Admin" or "CompanyAdmin")
                    {
                        var isSystemRestricted = screen is "Company Master" or "Company Setup" or "System Settings";
                        permission.CanView = true;
                        permission.CanEdit = !isSystemRestricted;
                        permission.CanDelete = !isSystemRestricted;
                    }
                    else if (roleName == "Manager")
                    {
                        if (moduleName != "Settings")
                        {
                            permission.CanView = true;
                            permission.CanEdit = true;
                            permission.CanDelete = true;
                        }
                        else
                        {
                            permission.CanView = true;
                        }
                    }
                    else if (roleName is "Sales User" or "Sales")
                    {
                        if (moduleName is "Sales" or "CRM")
                        {
                            permission.CanView = true;
                            permission.CanEdit = true;
                            permission.CanDelete = false;
                        }
                        else if (moduleName == "Inventory" || screen == "Customer Master")
                        {
                            permission.CanView = true;
                        }
                    }
                    else if (roleName is "Purchase User" or "Purchase")
                    {
                        if (moduleName is "Purchase" or "Inventory")
                        {
                            permission.CanView = true;
                            permission.CanEdit = true;
                            permission.CanDelete = false;
                        }
                        else if (screen == "Supplier Master")
                        {
                            permission.CanView = true;
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
                    else if (roleName == "Viewer")
                    {
                        permission.CanView = moduleName != "Settings";
                        permission.CanEdit = false;
                        permission.CanDelete = false;
                    }
                    else if (roleName == "Employee")
                    {
                        permission.CanView = moduleName is "Sales" or "Purchase" or "Inventory";
                        permission.CanEdit = false;
                        permission.CanDelete = false;
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
        var isSuperAdmin = User.IsInRole("Super Admin");
        var activeCompanyId = _companyContext.CurrentCompanyId;

        IQueryable<AppUser> userQuery = _userManager.Users.Include(u => u.Company);

        if (!isSuperAdmin)
        {
            if (!activeCompanyId.HasValue)
            {
                TempData["Error"] = "No active company context assigned.";
                return View(new UserPermissionsPageViewModel());
            }
            userQuery = userQuery.Where(u => u.CompanyId == activeCompanyId.Value);
        }

        var users = await userQuery.OrderBy(u => u.FullName).ToListAsync();

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
            Permissions = permissions,
            IsSuperAdmin = isSuperAdmin
        });
    }

    [HttpPost("SavePermissions")]
    public async Task<IActionResult> SavePermissions(string userId, List<ScreenPermissionDto> permissions)
    {
        var isSuperAdmin = User.IsInRole("Super Admin");
        var activeCompanyId = _companyContext.CurrentCompanyId;
        var currentUserName = User.Identity?.Name ?? "System";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(userId))
        {
            if (IsAjaxRequest()) return Json(ApiResponse.Fail("Invalid user specified."));
            TempData["Error"] = "Invalid user specified.";
            return RedirectToAction(nameof(Permissions));
        }

        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser == null)
        {
            if (IsAjaxRequest()) return Json(ApiResponse.Fail("User not found."));
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Permissions));
        }

        // Company boundary check for Company Admin
        if (!isSuperAdmin && targetUser.CompanyId != activeCompanyId)
        {
            await _loginHistoryService.RecordUserActivityAsync(
                targetUser.Id,
                currentUserName,
                "Company Admin",
                "SECURITY_VIOLATION",
                $"Unauthorized attempt to modify permissions of user '{targetUser.Email}' belonging to company ID {targetUser.CompanyId}",
                activeCompanyId,
                ip);

            if (IsAjaxRequest()) return Json(ApiResponse.Fail("Access Denied: You cannot modify permissions of users from another company."));
            TempData["Error"] = "Access Denied: You cannot modify permissions of users from another company.";
            return RedirectToAction(nameof(Permissions));
        }

        // Prevent modifying Super Admin permissions
        if (await _userManager.IsInRoleAsync(targetUser, "Super Admin"))
        {
            if (IsAjaxRequest()) return Json(ApiResponse.Fail("Super Admin permissions cannot be modified."));
            TempData["Error"] = "Super Admin permissions cannot be modified.";
            return RedirectToAction(nameof(Permissions), new { userId = userId });
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
                dbPerm.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ScreenPermissions.Add(new ScreenPermission
                {
                    UserId = userId,
                    ScreenName = pDto.ScreenName,
                    CanView = pDto.CanView,
                    CanEdit = pDto.CanEdit,
                    CanDelete = pDto.CanDelete,
                    CompanyId = targetUser.CompanyId
                });
            }
        }

        await _context.SaveChangesAsync();

        await _loginHistoryService.RecordUserActivityAsync(
            targetUser.Id,
            currentUserName,
            isSuperAdmin ? "Super Admin" : "Company Admin",
            "PERMISSIONS_UPDATED",
            $"Updated permissions for user '{targetUser.Email}'",
            targetUser.CompanyId,
            ip);

        if (IsAjaxRequest()) return Json(ApiResponse.Ok("User permissions updated successfully."));
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
