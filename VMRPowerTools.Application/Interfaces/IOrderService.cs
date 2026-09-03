using System.Collections.Generic;
using System.Threading.Tasks;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Application.Interfaces;

public class CheckoutRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Cash on Delivery";
    public string? Notes { get; set; }
    public string? UserId { get; set; }
}

public interface IOrderService
{
    Task<SalesOrder> CheckoutAsync(CheckoutRequest request, CartSummary cart);
    Task<IEnumerable<SalesOrder>> GetOrderHistoryAsync(string email);
    Task<SalesOrder?> GetOrderDetailsAsync(int orderId);
}
