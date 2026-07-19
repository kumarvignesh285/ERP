using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class Customer : BaseEntity
{
    [Required, MaxLength(20)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? MobileNumber { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? GSTNumber { get; set; }

    [MaxLength(15)]
    public string? PANNumber { get; set; }

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

    public decimal CreditLimit { get; set; }
    public decimal OpeningBalance { get; set; }

    [MaxLength(10)]
    public string BalanceType { get; set; } = "Dr";

    [MaxLength(200)]
    public string? ContactPerson { get; set; }

    [MaxLength(20)]
    public string? AlternatePhone { get; set; }

    public int? CreditDays { get; set; }
}
