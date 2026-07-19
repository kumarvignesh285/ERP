using System;
using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class CmsBlogPost : BaseEntity
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string Slug { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    [Required, MaxLength(500)]
    public string Summary { get; set; } = string.Empty;
    [Required]
    public string Content { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; } = DateTime.Today;
    [Required, MaxLength(100)]
    public string Author { get; set; } = "VMR Technical Desk";
    [MaxLength(500)]
    public string? ImagePath { get; set; }
}
