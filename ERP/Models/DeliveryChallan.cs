using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models;

public class DeliveryChallan : BaseEntity
{
    [Required, MaxLength(20)]
    public string ChallanNumber { get; set; } = string.Empty;
    public DateTime ChallanDate { get; set; } = DateTime.Today;
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? SalesOrderId { get; set; }
    [MaxLength(50)]
    public string? VehicleNumber { get; set; }
    [MaxLength(100)]
    public string? DriverName { get; set; }
    [MaxLength(500)]
    public string? DeliveryAddress { get; set; }
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
    public ICollection<DeliveryChallanItem> Items { get; set; } = new List<DeliveryChallanItem>();
}

public class DeliveryChallanItem
{
    public int Id { get; set; }
    public int DeliveryChallanId { get; set; }
    public DeliveryChallan? DeliveryChallan { get; set; }
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
