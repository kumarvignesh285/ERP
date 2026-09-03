using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models;

public class StockTransaction : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.Now;
    [Required, MaxLength(50)]
    public string TransactionType { get; set; } = "Purchase"; // Opening, Purchase, Sales, Return, Adjustment, Transfer
    [Required, MaxLength(50)]
    public string ReferenceNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; } // Positive for in, negative for out
    [Column(TypeName = "decimal(18,2)")]
    public decimal Rate { get; set; }
    [MaxLength(200)]
    public string? Remarks { get; set; }
}

public class StockTransfer : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(20)]
    public string TransferNumber { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; } = DateTime.Today;
    public int FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; }
    public int ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; }
    [MaxLength(200)]
    public string? ReferenceNumber { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "Completed"; // Pending, Completed
    [MaxLength(1000)]
    public string? Remarks { get; set; }
    public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
}

public class StockTransferItem
{
    public int Id { get; set; }
    public int StockTransferId { get; set; }
    public StockTransfer? StockTransfer { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }
    [MaxLength(200)]
    public string? Remarks { get; set; }
}

public class StockAdjustment : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(20)]
    public string AdjustmentNumber { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; } = DateTime.Today;
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    [MaxLength(50)]
    public string AdjustmentType { get; set; } = "Addition"; // Addition, Deduction
    [MaxLength(200)]
    public string? ReferenceNumber { get; set; }
    [MaxLength(1000)]
    public string? Remarks { get; set; }
    public ICollection<StockAdjustmentItem> Items { get; set; } = new List<StockAdjustmentItem>();
}

public class StockAdjustmentItem
{
    public int Id { get; set; }
    public int StockAdjustmentId { get; set; }
    public StockAdjustment? StockAdjustment { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Rate { get; set; }
    [MaxLength(200)]
    public string? Remarks { get; set; }
}

public class PhysicalStockVerification : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(20)]
    public string VerificationNumber { get; set; } = string.Empty;
    public DateTime VerificationDate { get; set; } = DateTime.Today;
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    [MaxLength(1000)]
    public string? Remarks { get; set; }
    public ICollection<PhysicalStockVerificationItem> Items { get; set; } = new List<PhysicalStockVerificationItem>();
}

public class PhysicalStockVerificationItem
{
    public int Id { get; set; }
    public int PhysicalStockVerificationId { get; set; }
    public PhysicalStockVerification? PhysicalStockVerification { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal BookStock { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal PhysicalStock { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal Variance { get; set; }
    [MaxLength(200)]
    public string? Remarks { get; set; }
}
