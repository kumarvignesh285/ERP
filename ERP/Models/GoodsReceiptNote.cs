using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models;

public class GoodsReceiptNote : BaseEntity
{
    [Required, MaxLength(20)]
    public string GRNNumber { get; set; } = string.Empty;
    public DateTime GRNDate { get; set; } = DateTime.Today;
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public int? PurchaseOrderId { get; set; }
    [MaxLength(50)]
    public string? ChalanNumber { get; set; }
    public DateTime? ChalanDate { get; set; }
    [MaxLength(50)]
    public string? VehicleNumber { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal GrandTotal { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";
    [MaxLength(1000)]
    public string? Notes { get; set; }
    public bool WithGST { get; set; } = false;
    public ICollection<GoodsReceiptNoteItem> Items { get; set; } = new List<GoodsReceiptNoteItem>();
}

public class GoodsReceiptNoteItem
{
    public int Id { get; set; }
    public int GoodsReceiptNoteId { get; set; }
    public GoodsReceiptNote? GoodsReceiptNote { get; set; }
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
