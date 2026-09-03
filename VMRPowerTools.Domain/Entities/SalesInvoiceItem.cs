using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class SalesInvoiceItem
{
    public int Id { get; set; }
    public int SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? HSNCode { get; set; }
    [MaxLength(20)]
    public string? UnitName { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Discount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}
