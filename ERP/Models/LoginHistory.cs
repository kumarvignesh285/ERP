using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models;

public class LoginHistory
{
    public int Id { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Role { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(20)]
    public string? CompanyCode { get; set; }

    public DateTime LoginTime { get; set; } = DateTime.UtcNow;

    public DateTime? LogoutTime { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Success"; // "Success", "Failed", "LoggedOut", "SessionExpired"

    [MaxLength(50)]
    public string? IPAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(50)]
    public string? Browser { get; set; }

    [MaxLength(50)]
    public string? OperatingSystem { get; set; }

    [MaxLength(50)]
    public string? Device { get; set; }

    [MaxLength(100)]
    public string? SessionId { get; set; }

    [MaxLength(250)]
    public string? FailureReason { get; set; }
}
