using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string module,
        string? entityName = null,
        string? entityId = null,
        string? description = null,
        object? oldValues = null,
        object? newValues = null,
        string status = "Success",
        string severity = "Info",
        int? companyId = null,
        string? correlationId = null);

    Task LogSecurityEventAsync(
        string action,
        string description,
        string severity = "Warning",
        int? companyId = null);

    Task LogCrudAsync(
        string action,
        string module,
        string entityName,
        string entityId,
        string description,
        object? oldValues = null,
        object? newValues = null,
        int? companyId = null);

    Task<(List<AuditLog> Items, int TotalCount, AuditLogStatisticsViewModel Statistics)> GetAuditLogsAsync(
        AuditLogFilterViewModel filter,
        ClaimsPrincipal currentUser);

    Task<AuditLogDetailDto?> GetAuditLogByIdAsync(int id, ClaimsPrincipal currentUser);

    Task<byte[]> ExportAuditLogsCsvAsync(AuditLogFilterViewModel filter, ClaimsPrincipal currentUser);
}
