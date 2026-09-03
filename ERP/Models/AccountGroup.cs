using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class AccountGroup : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(100)]
    public string GroupName { get; set; } = string.Empty;
    // Asset, Liability, Income, Expense, Equity
    [MaxLength(20)]
    public string GroupType { get; set; } = "Asset";
    [MaxLength(500)]
    public string? Description { get; set; }
    public int? ParentGroupId { get; set; }
    public AccountGroup? ParentGroup { get; set; }
    public ICollection<Ledger> Ledgers { get; set; } = new List<Ledger>();
}
