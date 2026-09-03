using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class Unit : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(50)]
    public string UnitName { get; set; } = string.Empty;
    [MaxLength(10)]
    public string UnitSymbol { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? Description { get; set; }
}
