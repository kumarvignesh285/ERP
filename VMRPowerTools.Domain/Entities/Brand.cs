using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class Brand : BaseEntity
{
    [Required, MaxLength(100)]
    public string BrandName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
