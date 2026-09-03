using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class Warehouse : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(20)]
    public string WarehouseCode { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string WarehouseName { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Address { get; set; }
    [MaxLength(100)]
    public string? City { get; set; }
    [MaxLength(100)]
    public string? State { get; set; }
    [MaxLength(200)]
    public string? ContactPerson { get; set; }
    [MaxLength(20)]
    public string? Phone { get; set; }
    [MaxLength(100)]
    public string? Email { get; set; }
}
