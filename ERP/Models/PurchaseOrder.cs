using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models;

public class PurchaseOrder : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(20)]
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.Today;
    public DateTime? DeliveryDate { get; set; }
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    [MaxLength(100)]
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
    public string Status { get; set; } = "Pending";
    [MaxLength(1000)]
    public string? Notes { get; set; }
    [MaxLength(200)]
    public string? DocumentPath { get; set; }
    public bool WithGST { get; set; } = false;
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

public class PurchaseOrderItem
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? UnitName { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal ReceivedQuantity { get; set; }
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
