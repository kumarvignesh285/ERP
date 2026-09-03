using System.Security.Claims;
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
        return View(new LoginViewModel { ReturnUrl = returnUrl ?? Url.Content("~/") });
    }

    [HttpPost]
    [Route("Account/Login")]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(string.Empty, "Email and password are required.");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        var user = await _userManager.FindByEmailAsync(email) ?? await _userManager.FindByNameAsync(email);
        if (user == null)
        {
            await _loginHistoryService.RecordFailedLoginAsync(email, "Invalid credentials", ip, userAgent);
            await _auditService.LogAsync(
                action: "LOGIN_FAILED",
                module: "Authentication",
                description: $"Failed login attempt for unknown user '{email}'",
                status: "Failed",
                severity: "Warning");

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
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
            return View(new LoginViewModel { ReturnUrl = returnUrl });
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
                return View(new LoginViewModel { ReturnUrl = returnUrl });
            }
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, password, rememberMe, lockoutOnFailure: false);
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

            if (returnUrl == Url.Content("~/"))
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

            return LocalRedirect(returnUrl);
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
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }
    }

    [HttpPost]
    [Route("Account/Logout")]
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

    [HttpGet]
    [Route("Account/AccessDenied")]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
