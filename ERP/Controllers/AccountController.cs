using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly IMasterService _masterService;
    private readonly ILoginHistoryService _loginHistoryService;
    private readonly IAuditService _auditService;

    public AccountController(
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        IMasterService masterService,
        ILoginHistoryService loginHistoryService,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _masterService = masterService;
        _loginHistoryService = loginHistoryService;
        _auditService = auditService;
    }

    [HttpGet]
    [Route("Account/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl ?? Url.Content("~/") });
    }

    [HttpPost]
    [Route("Account/Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginViewModel model)
    {
        model.ReturnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        var user = await _userManager.FindByEmailAsync(model.Email) ?? await _userManager.FindByNameAsync(model.Email);
        if (user == null)
        {
            await _loginHistoryService.RecordFailedLoginAsync(model.Email, "Invalid credentials", ip, userAgent);
            await _auditService.LogAsync(
                action: "LOGIN_FAILED",
                module: "Authentication",
                description: $"Failed login attempt for unknown user '{model.Email}'",
                status: "Failed",
                severity: "Warning");

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        if (!user.IsActive)
        {
            await _loginHistoryService.RecordFailedLoginAsync(user.UserName!, "Account deactivated", ip, userAgent, user.CompanyId);
            await _auditService.LogAsync(
                action: "LOGIN_FAILED",
                module: "Authentication",
                description: $"Login rejected for deactivated user account '{user.UserName}'",
                status: "Failed",
                severity: "Warning",
                companyId: user.CompanyId);

            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact your administrator.");
            return View(model);
        }

        if (user.CompanyId.HasValue && !await _userManager.IsInRoleAsync(user, "Super Admin"))
        {
            var company = await _masterService.GetCompanyByIdAsync(user.CompanyId.Value);
            if (company != null && !company.IsActive)
            {
                await _loginHistoryService.RecordFailedLoginAsync(user.UserName!, "Company account deactivated", ip, userAgent, user.CompanyId, company.CompanyCode);
                await _auditService.LogAsync(
                    action: "LOGIN_FAILED",
                    module: "Authentication",
                    description: $"Login rejected because company '{company.CompanyName}' is inactive",
                    status: "Failed",
                    severity: "Warning",
                    companyId: user.CompanyId);

                ModelState.AddModelError(string.Empty, "Your company account has been deactivated. Please contact the system administrator.");
                return View(model);
            }
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            HttpContext.Session.Clear();
            var sessionId = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString("UserSessionId", sessionId);

            await _loginHistoryService.RecordSuccessfulLoginAsync(user, sessionId, ip, userAgent);
            await _auditService.LogAsync(
                action: "LOGIN",
                module: "Authentication",
                entityName: "AppUser",
                entityId: user.Id,
                description: $"User '{user.UserName}' logged in successfully",
                status: "Success",
                severity: "Info",
                companyId: user.CompanyId);

            if (model.ReturnUrl == Url.Content("~/"))
            {
                if (await _userManager.IsInRoleAsync(user, "Sales User"))
                {
                    return RedirectToAction("Invoice", "Sales");
                }

                if (await _userManager.IsInRoleAsync(user, "Purchase User"))
                {
                    return RedirectToAction("Order", "Purchase");
                }

                if (await _userManager.IsInRoleAsync(user, "Accountant"))
                {
                    return RedirectToAction("CashBook", "Accounts");
                }
            }

            return LocalRedirect(model.ReturnUrl);
        }
        else
        {
            await _loginHistoryService.RecordFailedLoginAsync(user.UserName!, "Invalid credentials", ip, userAgent, user.CompanyId);
            await _auditService.LogAsync(
                action: "LOGIN_FAILED",
                module: "Authentication",
                description: $"Failed login attempt (wrong password) for user '{user.UserName}'",
                status: "Failed",
                severity: "Warning",
                companyId: user.CompanyId);

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }
    }

    [HttpPost]
    [Route("Account/Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var sessionId = HttpContext.Session.GetString("UserSessionId");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.Identity?.Name ?? "User";

        await _loginHistoryService.RecordLogoutAsync(userId, sessionId);
        await _auditService.LogAsync(
            action: "LOGOUT",
            module: "Authentication",
            entityName: "AppUser",
            entityId: userId,
            description: $"User '{userName}' logged out");

        HttpContext.Session.Clear();
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

    [Authorize]
    [HttpGet]
    [Route("Account/Profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var roles = await _userManager.GetRolesAsync(user);
        string? companyName = null;
        if (user.CompanyId.HasValue)
        {
            var company = await _masterService.GetCompanyByIdAsync(user.CompanyId.Value);
            companyName = company?.CompanyName;
        }

        var vm = new UserProfileViewModel
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Mobile = user.Mobile,
            Role = roles.FirstOrDefault() ?? "User",
            CompanyId = user.CompanyId,
            CompanyName = companyName,
            CreatedAt = user.CreatedAt
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [Route("Account/ChangePassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please correct the password validation errors.";
            return RedirectToAction("Profile");
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _auditService.LogAsync(
                action: "CHANGE_PASSWORD",
                module: "Authentication",
                entityName: "AppUser",
                entityId: user.Id,
                description: $"User '{user.UserName}' successfully changed their password.",
                status: "Success",
                severity: "Info",
                companyId: user.CompanyId);

            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToAction("Profile");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
        return RedirectToAction("Profile");
    }

    [HttpGet]
    [Route("Account/AccessDenied")]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
