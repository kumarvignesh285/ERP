using System;
using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class UserActivityLog
{
    public int Id { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Role { get; set; }

    [Required, MaxLength(50)]
    public string ActivityType { get; set; } = string.Empty; // "CompanyContextChanged", "SecurityDenied", etc.

    public int? PreviousCompanyId { get; set; }

    [MaxLength(20)]
    public string? PreviousCompanyCode { get; set; }

    public int? NewCompanyId { get; set; }

    [MaxLength(20)]
    public string? NewCompanyCode { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? IPAddress { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
