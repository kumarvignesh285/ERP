using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class Product : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(20)]
    public string ProductCode { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public int? UnitId { get; set; }
    public Unit? Unit { get; set; }
    public int? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    [MaxLength(20)]
    public string? HSNCode { get; set; }
    [MaxLength(50)]
    public string? Barcode { get; set; }
    [MaxLength(200)]
    public string? QRCode { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalesPrice { get; set; }
    public decimal MRP { get; set; }
    public decimal Discount { get; set; }
    public decimal GSTPercentage { get; set; }
    public decimal OpeningStock { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public decimal ReorderLevel { get; set; }
    [MaxLength(1000)]
    public string? Description { get; set; }
    [MaxLength(200)]
    public string? ImagePath { get; set; }
    public bool IsBatchTracked { get; set; }
    public bool IsSerialTracked { get; set; }
    [MaxLength(200)]
    public string? DocumentPath { get; set; }
}
