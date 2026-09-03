using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models;

public class Voucher : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(20)]
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    [Required, MaxLength(50)]
    public string VoucherType { get; set; } = "Journal"; // Receipt, Payment, Contra, Journal, DebitNote, CreditNote
    [MaxLength(50)]
    public string? ReferenceNumber { get; set; }
    [MaxLength(1000)]
    public string? Narration { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    public ICollection<VoucherItem> Items { get; set; } = new List<VoucherItem>();
}

public class VoucherItem
{
    public int Id { get; set; }
    public int VoucherId { get; set; }
    public Voucher? Voucher { get; set; }
    public int LedgerId { get; set; }
    public Ledger? Ledger { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal DebitAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditAmount { get; set; }
    [MaxLength(250)]
    public string? Particulars { get; set; }
    public int SortOrder { get; set; }
}
