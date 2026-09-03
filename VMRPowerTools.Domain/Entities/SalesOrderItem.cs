using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class SalesOrderItem
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? UnitName { get; set; }
    public decimal Quantity { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}
