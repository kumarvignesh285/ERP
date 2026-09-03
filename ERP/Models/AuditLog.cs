using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models;

public class AuditLog
{
    public int Id { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [Required, MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, VIEW, LOGIN, LOGOUT, SECURITY_WARNING, COMPANY_SWITCH

    [Required, MaxLength(50)]
    public string Module { get; set; } = string.Empty; // Products, Customers, Suppliers, Sales, Purchase, Settings, Security, Company

    [MaxLength(100)]
    public string? EntityName { get; set; }

    [MaxLength(100)]
    public string? EntityId { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? OldValues { get; set; } // JSON serialized diff (sensitive fields excluded)

    public string? NewValues { get; set; } // JSON serialized diff (sensitive fields excluded)

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(500)]
    public string? RequestPath { get; set; }

    [MaxLength(10)]
    public string? HttpMethod { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Success"; // Success, Failed

    [Required, MaxLength(20)]
    public string Severity { get; set; } = "Info"; // Info, Warning, Danger, Critical

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
