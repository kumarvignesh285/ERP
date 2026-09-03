using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class Brand : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(100)]
    public string BrandName { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
