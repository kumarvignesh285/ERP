using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VMRPowerTools.Application.Interfaces;

namespace VMRPowerTools.Website.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var cart = await _cartService.GetCartAsync();
            return View(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying shopping cart.");
            return View(new CartSummary());
        }
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        try
        {
            await _cartService.AddItemAsync(productId, quantity);
            return Json(new { success = true, message = "Item added to cart." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart.");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
    {
        try
        {
            await _cartService.UpdateQuantityAsync(productId, quantity);
            return Json(new { success = true, message = "Quantity updated." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart quantity.");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int productId)
    {
        try
        {
            await _cartService.RemoveItemAsync(productId);
            return Json(new { success = true, message = "Item removed from cart." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart.");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        try
        {
            await _cartService.ClearCartAsync();
            return Json(new { success = true, message = "Cart cleared." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart.");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ApplyCoupon(string couponCode)
    {
        try
        {
            var success = await _cartService.ApplyCouponAsync(couponCode);
            if (success)
            {
                return Json(new { success = true, message = "Coupon applied successfully." });
            }
            return Json(new { success = false, message = "Invalid coupon code." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying coupon.");
            return Json(new { success = false, message = ex.Message });
        }
    }
}
