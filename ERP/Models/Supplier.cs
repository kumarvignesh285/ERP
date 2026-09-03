using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class Supplier : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(20)]
    public string SupplierCode { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? GSTNumber { get; set; }
    [MaxLength(15)]
    public string? PANNumber { get; set; }
    [MaxLength(200)]
    public string? ContactPerson { get; set; }
    [MaxLength(20)]
    public string? Mobile { get; set; }
    [MaxLength(20)]
    public string? AlternatePhone { get; set; }
    [MaxLength(100)]
    public string? Email { get; set; }
    [MaxLength(500)]
    public string? Address { get; set; }
    [MaxLength(100)]
    public string? City { get; set; }
    [MaxLength(100)]
    public string? State { get; set; }
    [MaxLength(100)]
    public string? Country { get; set; } = "India";
    [MaxLength(10)]
    public string? Pincode { get; set; }
    public decimal OpeningBalance { get; set; }
    [MaxLength(10)]
    public string BalanceType { get; set; } = "Cr";
    public int? CreditDays { get; set; }
    public decimal CreditLimit { get; set; }

    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
}
