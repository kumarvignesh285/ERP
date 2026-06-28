using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class Bank : BaseEntity
{
    [Required, MaxLength(200)]
    public string BankName { get; set; } = string.Empty;
    [MaxLength(50)]
    public string? AccountNumber { get; set; }
    [MaxLength(20)]
    public string? IFSCCode { get; set; }
    [MaxLength(100)]
    public string? BranchName { get; set; }
    [MaxLength(50)]
    public string? AccountType { get; set; } = "Current";
    public decimal OpeningBalance { get; set; }
    public int? LedgerId { get; set; }
    public Ledger? Ledger { get; set; }
}
