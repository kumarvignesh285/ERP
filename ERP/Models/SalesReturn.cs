using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models;

public class SalesReturn : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(20)]
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; } = DateTime.Today;
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }
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
    public bool WithGST { get; set; } = false;
    public ICollection<SalesReturnItem> Items { get; set; } = new List<SalesReturnItem>();
}

public class SalesReturnItem
{
    public int Id { get; set; }
    public int SalesReturnId { get; set; }
    public SalesReturn? SalesReturn { get; set; }
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
