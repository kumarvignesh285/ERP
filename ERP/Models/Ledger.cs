using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class Ledger : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(20)]
    public string LedgerCode { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string LedgerName { get; set; } = string.Empty;
    public int AccountGroupId { get; set; }
    public AccountGroup? AccountGroup { get; set; }
    public decimal OpeningBalance { get; set; }
    [MaxLength(5)]
    public string BalanceType { get; set; } = "Dr";
    [MaxLength(50)]
    public string? BankAccountNumber { get; set; }
    [MaxLength(20)]
    public string? IFSCCode { get; set; }
    [MaxLength(100)]
    public string? BranchName { get; set; }
    [MaxLength(100)]
    public string? BankName { get; set; }
    public bool IsSystemLedger { get; set; }
    [MaxLength(500)]
    public string? Notes { get; set; }
    public ICollection<VoucherItem> VoucherItems { get; set; } = new List<VoucherItem>();
}
