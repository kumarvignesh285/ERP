using System.Collections.Generic;
using System.Threading.Tasks;

namespace VMRPowerTools.Application.Interfaces;

public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public int Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal SubTotal => Quantity * Rate;
    public decimal TaxAmount => SubTotal * (TaxPercentage / 100);
    public decimal TotalAmount => SubTotal + TaxAmount;
}

public class CartSummary
{
    public List<CartItem> Items { get; set; } = new List<CartItem>();
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCharge { get; set; }
    public decimal GrandTotal { get; set; }
    public string? CouponCode { get; set; }
    public string TaxBreakdown { get; set; } = string.Empty; // e.g. "CGST @ 9% + SGST @ 9%" or "IGST @ 18%"
}

public interface ICartService
{
    Task<CartSummary> GetCartAsync();
    Task AddItemAsync(int productId, int quantity);
    Task UpdateQuantityAsync(int productId, int quantity);
    Task RemoveItemAsync(int productId);
    Task ClearCartAsync();
    Task<bool> ApplyCouponAsync(string couponCode);
    Task<CartSummary> CalculateCheckoutTotalsAsync(string stateName);
}
