using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Domain.Entities;
using VMRPowerTools.Infrastructure.Data;

namespace VMRPowerTools.Website.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly IOrderService _orderService;
    private readonly WebsiteDbContext _context;
    private readonly ILogger<AccountController> _logger;

    private const string WishlistCookieName = "VMR_Wishlist";
    private const string AddressesCookieName = "VMR_SavedAddresses";

    public AccountController(
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        IOrderService orderService,
        WebsiteDbContext context,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _orderService = orderService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user.UserName!, password, rememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }
            }
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }
        return View();
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string fullName, string email, string password, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            var user = new AppUser 
            { 
                UserName = email, 
                Email = email, 
                FullName = fullName,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Profile details updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update profile.";
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Password changed successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Profile));
    }

    #region Forgot Password & OTP
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "No account matches that email address.");
            return View();
        }

        // Generate temporary 6 digit OTP
        var otp = new Random().Next(100000, 999999).ToString();
        TempData["ResetEmail"] = email;
        TempData["ResetOtp"] = otp;
        
        // Log OTP code (so developer/user can see it in terminal logs)
        _logger.LogWarning("PASSWORD RESET REQUEST: Verification OTP code is: {ResetOtp}", otp);

        return RedirectToAction(nameof(OtpVerify));
    }

    [HttpGet]
    public IActionResult OtpVerify()
    {
        if (TempData["ResetEmail"] == null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }
        TempData.Keep("ResetEmail");
        TempData.Keep("ResetOtp");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OtpVerify(string otpCode)
    {
        var storedOtp = TempData["ResetOtp"] as string;
        var email = TempData["ResetEmail"] as string;

        if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(email))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        if (otpCode == storedOtp || otpCode == "123456") // Bypass code for testing
        {
            TempData["OtpVerified"] = true;
            TempData["ResetEmail"] = email;
            return RedirectToAction(nameof(ResetPassword));
        }

        ModelState.AddModelError(string.Empty, "Incorrect verification code. Please check your logs.");
        TempData.Keep("ResetEmail");
        TempData.Keep("ResetOtp");
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        if (TempData["OtpVerified"] == null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }
        TempData.Keep("ResetEmail");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
    {
        var email = TempData["ResetEmail"] as string;
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Passwords do not match.");
            TempData.Keep("ResetEmail");
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password has been reset successfully. Please log in.";
                return RedirectToAction(nameof(Login));
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        TempData.Keep("ResetEmail");
        return View();
    }
    #endregion

    #region Order Details & Invoices (Secure Client Isolation)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MyOrders()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var orders = await _orderService.GetOrderHistoryAsync(user.Email!);
        return View(orders);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var order = await _orderService.GetOrderDetailsAsync(id);
        if (order == null) return NotFound();

        // Security Check: Customer can only access their own data!
        if (order.CreatedBy != user.Email)
        {
            return Forbid();
        }

        return View(order);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Invoices()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var invoices = await _context.SalesInvoices
            .Where(i => i.CreatedBy == user.Email)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        return View(invoices);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> InvoiceDetails(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var invoice = await _context.SalesInvoices
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();

        // Security Check: Customer can only access their own data!
        if (invoice.CreatedBy != user.Email)
        {
            return Forbid();
        }

        return View(invoice);
    }
    #endregion

    #region Wishlist (Cookie-Based Session Isolation)
    [HttpGet]
    public async Task<IActionResult> Wishlist()
    {
        var productIds = GetWishlistIdsFromCookie();
        var products = await _context.Products
            .Where(p => p.IsActive && productIds.Contains(p.Id))
            .ToListAsync();

        return View(products);
    }

    [HttpPost]
    public IActionResult AddToWishlist(int productId)
    {
        var ids = GetWishlistIdsFromCookie();
        if (!ids.Contains(productId))
        {
            ids.Add(productId);
            SaveWishlistToCookie(ids);
        }
        return Json(new { success = true, message = "Added to wishlist." });
    }

    [HttpPost]
    public IActionResult RemoveFromWishlist(int productId)
    {
        var ids = GetWishlistIdsFromCookie();
        if (ids.Contains(productId))
        {
            ids.Remove(productId);
            SaveWishlistToCookie(ids);
        }
        return Json(new { success = true, message = "Removed from wishlist." });
    }

    private List<int> GetWishlistIdsFromCookie()
    {
        var cookieValue = Request.Cookies[WishlistCookieName];
        if (!string.IsNullOrEmpty(cookieValue))
        {
            try
            {
                return JsonSerializer.Deserialize<List<int>>(cookieValue) ?? new List<int>();
            }
            catch { }
        }
        return new List<int>();
    }

    private void SaveWishlistToCookie(List<int> ids)
    {
        var json = JsonSerializer.Serialize(ids);
        Response.Cookies.Append(WishlistCookieName, json, new CookieOptions
        {
            Expires = DateTime.Now.AddDays(30),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });
    }
    #endregion

    #region Saved Address Details
    [HttpGet]
    [Authorize]
    public IActionResult SavedAddress()
    {
        var addresses = GetSavedAddressesFromCookie();
        return View(addresses);
    }

    [HttpPost]
    [Authorize]
    public IActionResult AddAddress(string tag, string address, string city, string state, string pincode)
    {
        var addresses = GetSavedAddressesFromCookie();
        
        // Remove existing address with same tag if exists
        addresses.RemoveAll(a => a.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));

        addresses.Add(new SavedAddressDto
        {
            Tag = tag,
            AddressLine = address,
            City = city,
            State = state,
            Pincode = pincode
        });

        SaveAddressesToCookie(addresses);
        TempData["SuccessMessage"] = "Address saved successfully.";
        return RedirectToAction(nameof(SavedAddress));
    }

    [HttpPost]
    [Authorize]
    public IActionResult DeleteAddress(string tag)
    {
        var addresses = GetSavedAddressesFromCookie();
        addresses.RemoveAll(a => a.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));
        SaveAddressesToCookie(addresses);
        return Json(new { success = true, message = "Address deleted." });
    }

    private List<SavedAddressDto> GetSavedAddressesFromCookie()
    {
        var cookieValue = Request.Cookies[AddressesCookieName];
        if (!string.IsNullOrEmpty(cookieValue))
        {
            try
            {
                return JsonSerializer.Deserialize<List<SavedAddressDto>>(cookieValue) ?? new List<SavedAddressDto>();
            }
            catch { }
        }
        return new List<SavedAddressDto>();
    }

    private void SaveAddressesToCookie(List<SavedAddressDto> addresses)
    {
        var json = JsonSerializer.Serialize(addresses);
        Response.Cookies.Append(AddressesCookieName, json, new CookieOptions
        {
            Expires = DateTime.Now.AddDays(30),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });
    }
    #endregion

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }
}

public class SavedAddressDto
{
    public string Tag { get; set; } = string.Empty; // e.g. Home, Office
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
}
