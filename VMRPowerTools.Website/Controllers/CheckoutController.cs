using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Website.Controllers;

public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        ICartService cartService,
        IOrderService orderService,
        UserManager<AppUser> userManager,
        ILogger<CheckoutController> logger)
    {
        _cartService = cartService;
        _orderService = orderService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var cart = await _cartService.GetCartAsync();
            if (cart.Items == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var request = new CheckoutRequest();
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    request.Name = user.FullName;
                    request.Email = user.Email!;
                    request.Phone = user.PhoneNumber ?? string.Empty;
                    request.UserId = user.Id;
                }
            }

            ViewBag.Cart = cart;
            return View(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating checkout process.");
            return RedirectToAction("Index", "Cart");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTotals(string stateName)
    {
        try
        {
            var cart = await _cartService.CalculateCheckoutTotalsAsync(stateName);
            return Json(new { success = true, subTotal = cart.SubTotal, discount = cart.DiscountAmount, tax = cart.TaxAmount, shipping = cart.ShippingCharge, grandTotal = cart.GrandTotal, breakdown = cart.TaxBreakdown });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating checkout totals.");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutRequest request)
    {
        try
        {
            var cart = await _cartService.GetCartAsync();
            if (cart.Items == null || !cart.Items.Any())
            {
                ModelState.AddModelError(string.Empty, "Your cart is empty.");
                ViewBag.Cart = cart;
                return View("Index", request);
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    request.UserId = user.Id;
                    request.Email = user.Email!;
                }
            }

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Address))
            {
                ModelState.AddModelError(string.Empty, "Please fill in all required delivery fields.");
                ViewBag.Cart = cart;
                return View("Index", request);
            }

            // Recalculate totals for selected state before placing order
            cart = await _cartService.CalculateCheckoutTotalsAsync(request.State);

            // Execute transactional checkout
            var order = await _orderService.CheckoutAsync(request, cart);

            // Clear Cart
            await _cartService.ClearCartAsync();

            return RedirectToAction(nameof(Success), new { id = order.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order placement checkout.");
            ModelState.AddModelError(string.Empty, "An error occurred while placing your order. Please try again.");
            ViewBag.Cart = await _cartService.GetCartAsync();
            return View("Index", request);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Success(int id)
    {
        try
        {
            var order = await _orderService.GetOrderDetailsAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading order success details for Id {OrderId}.", id);
            return RedirectToAction("Index", "Home");
        }
    }
}
