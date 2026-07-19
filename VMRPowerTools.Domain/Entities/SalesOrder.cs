using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VMRPowerTools.Domain.Entities;

public class SalesOrder : BaseEntity
{
    [Required, MaxLength(20)]
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.Today;
    public DateTime? DeliveryDate { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    [MaxLength(100)]
    public string? Salesperson { get; set; }
    [MaxLength(50)]
    public string? ReferenceNumber { get; set; }
    public int? QuotationId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrandTotal { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";
    [MaxLength(500)]
    public string? ShippingAddress { get; set; }
    [MaxLength(1000)]
    public string? Notes { get; set; }
    public bool WithGST { get; set; } = false;
    public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
}
