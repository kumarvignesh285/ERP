using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class Company : BaseEntity
{
    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;
    [Required, MaxLength(50)]
    public string CompanyCode { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Address { get; set; }
    [MaxLength(100)]
    public string? City { get; set; }
    [MaxLength(100)]
    public string? State { get; set; }
    [MaxLength(100)]
    public string? Country { get; set; } = "India";
    [MaxLength(20)]
    public string? Pincode { get; set; }
    [MaxLength(20)]
    public string? Phone { get; set; }
    [MaxLength(100)]
    public string? Email { get; set; }
    [MaxLength(200)]
    public string? Website { get; set; }
    [MaxLength(20)]
    public string? GSTNumber { get; set; }
    [MaxLength(15)]
    public string? PANNumber { get; set; }
    [MaxLength(200)]
    public string? Logo { get; set; }
    [MaxLength(50)]
    public string BillType { get; set; } = "Tax Invoice";
    [MaxLength(20)]
    public string SalesBillPrefix { get; set; } = "INV-";
    public int SalesBillStartNumber { get; set; } = 1;
    public int SalesBillNextNumber { get; set; } = 1;
    [MaxLength(20)]
    public string PurchaseBillPrefix { get; set; } = "PINV-";
    public int PurchaseBillStartNumber { get; set; } = 1;
    public int PurchaseBillNextNumber { get; set; } = 1;
    [MaxLength(500)]
    public string? BillFooterNote { get; set; }
    public DateTime FinancialYearStart { get; set; } = new DateTime(DateTime.Now.Year, 4, 1);
    [MaxLength(50)]
    public string? CINNumber { get; set; }
    [MaxLength(500)]
    public string? BankDetails { get; set; }
    [MaxLength(50)]
    public string? TAN { get; set; }
    [MaxLength(10)]
    public string Currency { get; set; } = "INR";
    [MaxLength(20)]
    public string? FinancialYear { get; set; }
    [MaxLength(100)]
    public string? BusinessType { get; set; }
    [MaxLength(20)]
    public string? AlternatePhone { get; set; }
}
