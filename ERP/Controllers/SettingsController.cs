using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public SettingsController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IMasterService masterService, IWebHostEnvironment env)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _masterService = masterService;
        _env = env;
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
                EmailConfirmed = true
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
}
