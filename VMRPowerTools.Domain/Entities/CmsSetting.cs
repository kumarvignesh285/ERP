using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class CmsSetting : BaseEntity
{
    [Required, MaxLength(100)]
    public string SettingKey { get; set; } = string.Empty;
    [Required]
    public string SettingValue { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? Description { get; set; }
}
