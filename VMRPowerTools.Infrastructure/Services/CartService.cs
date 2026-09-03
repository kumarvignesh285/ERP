using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Domain.Entities;
using VMRPowerTools.Infrastructure.Data;

namespace VMRPowerTools.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly WebsiteDbContext _context;
    private const string CartCookieName = "VMR_Cart";
    private const string CouponCookieName = "VMR_Coupon";
    private const string CheckoutStateCookieName = "VMR_CheckoutState";

    public CartService(IHttpContextAccessor httpContextAccessor, WebsiteDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    private HttpContext HttpContext => _httpContextAccessor.HttpContext 
        ?? throw new InvalidOperationException("HttpContext is not available.");

    public async Task<CartSummary> GetCartAsync()
    {
        var cartCookie = HttpContext.Request.Cookies[CartCookieName];
        var couponCode = HttpContext.Request.Cookies[CouponCookieName];
        var stateName = HttpContext.Request.Cookies[CheckoutStateCookieName] ?? "Tamil Nadu";

        var items = new List<CartItem>();
        if (!string.IsNullOrEmpty(cartCookie))
        {
            try
            {
                var storedItems = JsonSerializer.Deserialize<List<CartItem>>(cartCookie);
                if (storedItems != null)
                {
                    // Refresh prices and taxes from the actual DB records to prevent client-side tampering
                    foreach (var item in storedItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null && product.IsActive)
                        {
                            items.Add(new CartItem
                            {
                                ProductId = product.Id,
                                ProductName = product.ProductName,
                                ProductCode = product.ProductCode,
                                ImagePath = product.ImagePath,
                                Rate = product.SalesPrice,
                                TaxPercentage = product.GSTPercentage,
                                Quantity = item.Quantity
                            });
                        }
                    }
                }
            }
            catch
            {
                // Fallback / log corrupted cookie
            }
        }

        var subTotal = items.Sum(i => i.SubTotal);
        
        // Calculate Discount
        decimal discount = 0;
        if (!string.IsNullOrEmpty(couponCode))
        {
            discount = couponCode.ToUpper() switch
            {
                "VMR10" => subTotal * 0.10m,
                "WELCOME15" => subTotal * 0.15m,
                "FREE500" => subTotal > 1000 ? 500m : 0m,
                _ => 0m
            };
        }

        // Calculate GST Tax
        decimal tax = 0;
        string breakdown = "GST Included";
        bool isLocalState = stateName.Trim().ToLower() == "tamil nadu";
        
        foreach (var item in items)
        {
            var itemSubTotalAfterDiscount = item.SubTotal - (subTotal > 0 ? (item.SubTotal / subTotal) * discount : 0);
            var itemTax = itemSubTotalAfterDiscount * (item.TaxPercentage / 100);
            tax += itemTax;
        }

        if (isLocalState)
        {
            breakdown = "CGST @ 9% + SGST @ 9%";
        }
        else
        {
            breakdown = "IGST @ 18%";
        }

        // Calculate Shipping Charge (Free above 5000, else flat 250)
        decimal shipping = subTotal > 0 && (subTotal - discount) < 5000 ? 250m : 0m;

        var grandTotal = subTotal - discount + tax + shipping;

        return new CartSummary
        {
            Items = items,
            SubTotal = subTotal,
            DiscountAmount = discount,
            TaxAmount = tax,
            ShippingCharge = shipping,
            GrandTotal = Math.Round(grandTotal, 2),
            CouponCode = couponCode,
            TaxBreakdown = breakdown
        };
    }

    public async Task AddItemAsync(int productId, int quantity)
    {
        var summary = await GetCartAsync();
        var items = summary.Items;

        var existingItem = items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                items.Add(new CartItem
                {
                    ProductId = product.Id,
                    Quantity = quantity
                });
            }
        }

        SaveCartToCookie(items);
        await Task.CompletedTask;
    }

    public async Task UpdateQuantityAsync(int productId, int quantity)
    {
        var summary = await GetCartAsync();
        var items = summary.Items;

        var existingItem = items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            if (quantity <= 0)
            {
                items.Remove(existingItem);
            }
            else
            {
                existingItem.Quantity = quantity;
            }
            SaveCartToCookie(items);
        }
        await Task.CompletedTask;
    }

    public async Task RemoveItemAsync(int productId)
    {
        var summary = await GetCartAsync();
        var items = summary.Items;

        var existingItem = items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            items.Remove(existingItem);
            SaveCartToCookie(items);
        }
        await Task.CompletedTask;
    }

    public async Task ClearCartAsync()
    {
        HttpContext.Response.Cookies.Delete(CartCookieName);
        HttpContext.Response.Cookies.Delete(CouponCookieName);
        HttpContext.Response.Cookies.Delete(CheckoutStateCookieName);
        await Task.CompletedTask;
    }

    public async Task<bool> ApplyCouponAsync(string couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            HttpContext.Response.Cookies.Delete(CouponCookieName);
            return true;
        }

        var validCoupons = new[] { "VMR10", "WELCOME15", "FREE500" };
        var cleanCode = couponCode.Trim().ToUpper();
        if (validCoupons.Contains(cleanCode))
        {
            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(1),
                HttpOnly = true,
                Secure = true
            };
            HttpContext.Response.Cookies.Append(CouponCookieName, cleanCode, options);
            return await Task.FromResult(true);
        }

        return await Task.FromResult(false);
    }

    public async Task<CartSummary> CalculateCheckoutTotalsAsync(string stateName)
    {
        var options = new CookieOptions
        {
            Expires = DateTime.Now.AddDays(1),
            HttpOnly = true,
            Secure = true
        };
        HttpContext.Response.Cookies.Append(CheckoutStateCookieName, stateName ?? "Tamil Nadu", options);
        return await GetCartAsync();
    }

    private void SaveCartToCookie(List<CartItem> items)
    {
        var serializable = items.Select(i => new CartItem { ProductId = i.ProductId, Quantity = i.Quantity }).ToList();
        var json = JsonSerializer.Serialize(serializable);
        var options = new CookieOptions
        {
            Expires = DateTime.Now.AddDays(7),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        };
        HttpContext.Response.Cookies.Append(CartCookieName, json, options);
    }
}
