using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class SalesInvoice : BaseEntity
{
    [Required, MaxLength(20)]
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public DateTime? DueDate { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    [MaxLength(100)]
    public string? Salesperson { get; set; }
    [MaxLength(50)]
    public string? ReferenceNumber { get; set; }
    [MaxLength(100)]
    public string? PaymentTerms { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RoundOff { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "Draft";
    [MaxLength(1000)]
    public string? Notes { get; set; }
    public bool IsPrinted { get; set; }
    public int? SalesOrderId { get; set; }
    public bool WithGST { get; set; } = false;
    public ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();
}
