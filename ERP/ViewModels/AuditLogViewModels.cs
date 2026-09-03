using System;
using System.Collections.Generic;
using ERP.Models;

namespace ERP.ViewModels;

public class AuditLogFilterViewModel
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? CompanyId { get; set; }
    public string? Module { get; set; }
    public string? Action { get; set; }
    public string? Severity { get; set; }
    public string? Username { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class AuditLogStatisticsViewModel
{
    public int TotalLogs { get; set; }
    public int SecurityEvents { get; set; }
    public int CrudChanges { get; set; }
    public int ContextSwitches { get; set; }
}

public class AuditLogPageViewModel
{
    public List<AuditLog> Logs { get; set; } = new();
    public AuditLogFilterViewModel Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public bool IsSuperAdmin { get; set; }
    public string CurrentUserRole { get; set; } = "CompanyUser";
    public int? UserCompanyId { get; set; }
    public List<Company> AvailableCompanies { get; set; } = new();
    public AuditLogStatisticsViewModel Statistics { get; set; } = new();
    public List<string> AvailableModules { get; set; } = new();
    public List<string> AvailableActions { get; set; } = new();
}

public class AuditLogFieldDiff
{
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public bool IsChanged => OldValue != NewValue;
}

public class AuditLogDetailDto
{
    public int Id { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public string Status { get; set; } = "Success";
    public string Severity { get; set; } = "Info";
    public string? CorrelationId { get; set; }
    public string TimestampFormatted { get; set; } = string.Empty;
    public List<AuditLogFieldDiff> FieldDiffs { get; set; } = new();
    public string? RawOldValues { get; set; }
    public string? RawNewValues { get; set; }
}
