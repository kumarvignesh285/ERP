using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class Tax : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(50)]
    public string TaxName { get; set; } = string.Empty;
    public decimal TaxPercentage { get; set; }
    [MaxLength(20)]
    public string TaxType { get; set; } = "GST";
    [MaxLength(200)]
    public string? Description { get; set; }
    public decimal? CGSTPercentage { get; set; }
    public decimal? SGSTPercentage { get; set; }
    public decimal? IGSTPercentage { get; set; }
}

public class PaymentMode : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(50)]
    public string ModeName { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? Description { get; set; }
    [MaxLength(20)]
    public string ModeType { get; set; } = "Cash";
}
