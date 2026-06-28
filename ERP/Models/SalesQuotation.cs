using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models;

public class SalesQuotation : BaseEntity
{
    [Required, MaxLength(20)]
    public string QuotationNumber { get; set; } = string.Empty;
    public DateTime QuotationDate { get; set; } = DateTime.Today;
    public DateTime? ValidUntil { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    [MaxLength(100)]
    public string? Salesperson { get; set; }
    [MaxLength(50)]
    public string? ReferenceNumber { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal GrandTotal { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "Draft";
    [MaxLength(1000)]
    public string? Notes { get; set; }
    public ICollection<SalesQuotationItem> Items { get; set; } = new List<SalesQuotationItem>();
}

public class SalesQuotationItem
{
    public int Id { get; set; }
    public int SalesQuotationId { get; set; }
    public SalesQuotation? SalesQuotation { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? UnitName { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Rate { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Discount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxPercentage { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}
