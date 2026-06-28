using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
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

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Email and Password are required.");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && !user.IsActive)
            {
                await _signInManager.SignOutAsync();
                ModelState.AddModelError(string.Empty, "Your account is inactive. Please contact the administrator.");
                return View(new LoginViewModel { ReturnUrl = returnUrl });
            }

            if (returnUrl == Url.Content("~/") && user != null)
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
            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }
    }

    [HttpPost]
    [Route("Account/Logout")]
    public async Task<IActionResult> Logout()
    {
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
