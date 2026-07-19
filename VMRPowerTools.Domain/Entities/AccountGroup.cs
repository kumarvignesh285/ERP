using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class AccountGroup : BaseEntity
{
    [Required, MaxLength(100)]
    public string GroupName { get; set; } = string.Empty;
    [MaxLength(20)]
    public string GroupType { get; set; } = "Asset";
    [MaxLength(500)]
    public string? Description { get; set; }
    public int? ParentGroupId { get; set; }
    public AccountGroup? ParentGroup { get; set; }
    public ICollection<Ledger> Ledgers { get; set; } = new List<Ledger>();
}
