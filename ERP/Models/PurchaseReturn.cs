using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models;

public class PurchaseReturn : BaseEntity
{
    [Required, MaxLength(20)]
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; } = DateTime.Today;
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public int? PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }
    [MaxLength(200)]
    public string? Reason { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal GrandTotal { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "Draft";
    [MaxLength(1000)]
    public string? Notes { get; set; }
    public ICollection<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
}

public class PurchaseReturnItem
{
    public int Id { get; set; }
    public int PurchaseReturnId { get; set; }
    public PurchaseReturn? PurchaseReturn { get; set; }
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
    public decimal TaxPercentage { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}
