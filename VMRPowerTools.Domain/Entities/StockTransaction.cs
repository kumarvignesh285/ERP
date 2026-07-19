using System;
using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class StockTransaction : BaseEntity
{
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    [Required, MaxLength(50)]
    public string TransactionType { get; set; } = "Sales";
    [Required, MaxLength(50)]
    public string ReferenceNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int? WarehouseId { get; set; }
    public decimal Quantity { get; set; } // Negative for sales dispatches
    public decimal Rate { get; set; }
    [MaxLength(200)]
    public string? Remarks { get; set; }
}
