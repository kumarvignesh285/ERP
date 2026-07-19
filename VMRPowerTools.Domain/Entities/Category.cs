using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class Category : BaseEntity
{
    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
