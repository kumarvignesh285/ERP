using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class Category : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
