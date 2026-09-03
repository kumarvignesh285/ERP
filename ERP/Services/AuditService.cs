using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICompanyContext _companyContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AuditService> _logger;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwordhash", "securitystamp", "concurrencystamp",
        "cleartextpassword", "token", "refreshtoken", "secret", "secretkey",
        "cardnumber", "cvv", "connectionstring"
    };

    public AuditService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ICompanyContext companyContext,
        UserManager<AppUser> userManager,
        ILogger<AuditService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _companyContext = companyContext;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task LogAsync(
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
        string? correlationId = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;

            string? userId = null;
            string userName = "System";

            if (user != null && user.Identity?.IsAuthenticated == true)
            {
                userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                userName = user.Identity.Name ?? "AuthenticatedUser";
            }

            int? targetCompanyId = companyId;
            if (!targetCompanyId.HasValue)
            {
                targetCompanyId = _companyContext.CurrentCompanyId;
            }

            // Super Admin system events with companyId = 0 are treated as null (system-wide)
            if (targetCompanyId == 0)
            {
                targetCompanyId = null;
            }

            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();
            var requestPath = httpContext?.Request.Path.ToString();
            var httpMethod = httpContext?.Request.Method;

            var oldJson = SerializeAndRedact(oldValues);
            var newJson = SerializeAndRedact(newValues);

            var auditLog = new AuditLog
            {
                CompanyId = targetCompanyId,
                UserId = userId,
                UserName = userName,
                Action = action.ToUpperInvariant(),
                Module = module,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                OldValues = oldJson,
                NewValues = newJson,
                IpAddress = ipAddress,
                UserAgent = userAgent != null && userAgent.Length > 500 ? userAgent.Substring(0, 500) : userAgent,
                RequestPath = requestPath,
                HttpMethod = httpMethod,
                Status = status,
                Severity = severity,
                CorrelationId = correlationId ?? httpContext?.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for Action: {Action}, Module: {Module}.", action, module);
        }
    }

    public Task LogSecurityEventAsync(string action, string description, string severity = "Warning", int? companyId = null)
    {
        return LogAsync(
            action: action,
            module: "Security",
            description: description,
            severity: severity,
            companyId: companyId);
    }

    public Task LogCrudAsync(
        string action,
        string module,
        string entityName,
        string entityId,
        string description,
        object? oldValues = null,
        object? newValues = null,
        int? companyId = null)
    {
        return LogAsync(
            action: action,
            module: module,
            entityName: entityName,
            entityId: entityId,
            description: description,
            oldValues: oldValues,
            newValues: newValues,
            severity: action.Equals("DELETE", StringComparison.OrdinalIgnoreCase) ? "Warning" : "Info",
            companyId: companyId);
    }

    public async Task<(List<AuditLog> Items, int TotalCount, AuditLogStatisticsViewModel Statistics)> GetAuditLogsAsync(
        AuditLogFilterViewModel filter,
        ClaimsPrincipal currentUser)
    {
        var isSuperAdmin = currentUser.IsInRole("Super Admin");
        var activeCompanyId = _companyContext.CurrentCompanyId;

        IQueryable<AuditLog> query = _context.AuditLogs.IgnoreQueryFilters().Include(a => a.Company);

        // Multi-Tenant Isolation Filter
        if (!isSuperAdmin)
        {
            if (activeCompanyId.HasValue && activeCompanyId.Value > 0)
            {
                query = query.Where(a => a.CompanyId == activeCompanyId.Value);
            }
            else
            {
                var userClaimCompId = currentUser.FindFirst("CompanyId")?.Value;
                if (int.TryParse(userClaimCompId, out var cid) && cid > 0)
                {
                    query = query.Where(a => a.CompanyId == cid);
                }
                else
                {
                    return (new List<AuditLog>(), 0, new AuditLogStatisticsViewModel());
                }
            }
        }
        else
        {
            // Super Admin filtering
            if (filter.CompanyId.HasValue)
            {
                if (filter.CompanyId.Value == -1)
                {
                    query = query.Where(a => a.CompanyId == null);
                }
                else if (filter.CompanyId.Value > 0)
                {
                    query = query.Where(a => a.CompanyId == filter.CompanyId.Value);
                }
            }
        }

        // Statistics computation (before pagination and non-temporal filters)
        var statQuery = query;
        if (filter.DateFrom.HasValue)
        {
            var fromUtc = filter.DateFrom.Value.Date;
            statQuery = statQuery.Where(a => a.Timestamp >= fromUtc);
        }
        if (filter.DateTo.HasValue)
        {
            var toUtc = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);
            statQuery = statQuery.Where(a => a.Timestamp <= toUtc);
        }

        var totalLogs = await statQuery.CountAsync();
        var securityEvents = await statQuery.CountAsync(a => a.Module == "Security" || a.Severity == "Warning" || a.Severity == "Danger" || a.Severity == "Critical");
        var crudChanges = await statQuery.CountAsync(a => a.Action == "CREATE" || a.Action == "UPDATE" || a.Action == "DELETE");
        var contextSwitches = await statQuery.CountAsync(a => a.Action == "COMPANY_SWITCH");

        var stats = new AuditLogStatisticsViewModel
        {
            TotalLogs = totalLogs,
            SecurityEvents = securityEvents,
            CrudChanges = crudChanges,
            ContextSwitches = contextSwitches
        };

        // Applied Filter Specifications
        if (filter.DateFrom.HasValue)
        {
            var fromUtc = filter.DateFrom.Value.Date;
            query = query.Where(a => a.Timestamp >= fromUtc);
        }
        if (filter.DateTo.HasValue)
        {
            var toUtc = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.Timestamp <= toUtc);
        }
        if (!string.IsNullOrWhiteSpace(filter.Module))
        {
            query = query.Where(a => a.Module == filter.Module.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(a => a.Action == filter.Action.Trim().ToUpper());
        }
        if (!string.IsNullOrWhiteSpace(filter.Severity))
        {
            query = query.Where(a => a.Severity == filter.Severity.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filter.Username))
        {
            query = query.Where(a => a.UserName.Contains(filter.Username.Trim()));
        }
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(a =>
                a.Description!.Contains(term) ||
                a.EntityName!.Contains(term) ||
                a.EntityId!.Contains(term) ||
                a.UserName.Contains(term));
        }

        var totalFiltered = await query.CountAsync();

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 5 ? 25 : (filter.PageSize > 100 ? 100 : filter.PageSize);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalFiltered, stats);
    }

    public async Task<AuditLogDetailDto?> GetAuditLogByIdAsync(int id, ClaimsPrincipal currentUser)
    {
        var isSuperAdmin = currentUser.IsInRole("Super Admin");
        var activeCompanyId = _companyContext.CurrentCompanyId;

        var log = await _context.AuditLogs
            .IgnoreQueryFilters()
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (log == null) return null;

        // Tenant Security Check
        if (!isSuperAdmin)
        {
            if (activeCompanyId.HasValue && log.CompanyId != activeCompanyId.Value)
            {
                return null; // Tenant mismatch: block access
            }
        }

        var dto = new AuditLogDetailDto
        {
            Id = log.Id,
            CompanyId = log.CompanyId,
            CompanyName = log.Company != null ? $"{log.Company.CompanyName} ({log.Company.CompanyCode})" : (log.CompanyId.HasValue ? $"Company #{log.CompanyId}" : "System-Wide"),
            UserId = log.UserId,
            UserName = log.UserName,
            Action = log.Action,
            Module = log.Module,
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            Description = log.Description,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            RequestPath = log.RequestPath,
            HttpMethod = log.HttpMethod,
            Status = log.Status,
            Severity = log.Severity,
            CorrelationId = log.CorrelationId,
            TimestampFormatted = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            RawOldValues = log.OldValues,
            RawNewValues = log.NewValues,
            FieldDiffs = CalculateFieldDiffs(log.OldValues, log.NewValues)
        };

        return dto;
    }

    public async Task<byte[]> ExportAuditLogsCsvAsync(AuditLogFilterViewModel filter, ClaimsPrincipal currentUser)
    {
        filter.Page = 1;
        filter.PageSize = 5000; // Cap export

        var (items, _, _) = await GetAuditLogsAsync(filter, currentUser);

        var sb = new StringBuilder();
        sb.AppendLine("ID,Timestamp (UTC),User,Company,Module,Action,Entity,Entity ID,Severity,Status,IP Address,Description");

        foreach (var log in items)
        {
            var company = log.Company != null ? $"{log.Company.CompanyCode}" : (log.CompanyId.HasValue ? $"ID:{log.CompanyId}" : "System");
            var description = EscapeCsv(log.Description ?? string.Empty);
            var entity = EscapeCsv(log.EntityName ?? string.Empty);
            var entityId = EscapeCsv(log.EntityId ?? string.Empty);

            sb.AppendLine($"{log.Id},{log.Timestamp:yyyy-MM-dd HH:mm:ss},{EscapeCsv(log.UserName)},{company},{EscapeCsv(log.Module)},{log.Action},{entity},{entityId},{log.Severity},{log.Status},{log.IpAddress},{description}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string? SerializeAndRedact(object? obj)
    {
        if (obj == null) return null;

        if (obj is string str)
        {
            return str;
        }

        try
        {
            var json = JsonSerializer.Serialize(obj);
            var node = JsonNode.Parse(json);
            if (node is JsonObject jsonObj)
            {
                RedactJsonObject(jsonObj);
                return jsonObj.ToJsonString();
            }
            return json;
        }
        catch
        {
            return null;
        }
    }

    private static void RedactJsonObject(JsonObject jsonObj)
    {
        var keysToRemove = new List<string>();
        foreach (var kvp in jsonObj.ToList())
        {
            if (SensitiveKeys.Contains(kvp.Key))
            {
                keysToRemove.Add(kvp.Key);
            }
            else if (kvp.Value is JsonObject childObj)
            {
                RedactJsonObject(childObj);
            }
        }

        foreach (var key in keysToRemove)
        {
            jsonObj[key] = "[REDACTED]";
        }
    }

    private static List<AuditLogFieldDiff> CalculateFieldDiffs(string? oldJson, string? newJson)
    {
        var diffs = new List<AuditLogFieldDiff>();
        var oldDict = ParseJsonToDictionary(oldJson);
        var newDict = ParseJsonToDictionary(newJson);

        var allKeys = new HashSet<string>(oldDict.Keys.Concat(newDict.Keys), StringComparer.OrdinalIgnoreCase);

        foreach (var key in allKeys)
        {
            oldDict.TryGetValue(key, out var oldVal);
            newDict.TryGetValue(key, out var newVal);

            if (oldVal != newVal)
            {
                diffs.Add(new AuditLogFieldDiff
                {
                    FieldName = key,
                    OldValue = oldVal ?? "(None)",
                    NewValue = newVal ?? "(None)"
                });
            }
        }

        return diffs;
    }

    private static Dictionary<string, string?> ParseJsonToDictionary(string? json)
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json)) return dict;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ToString();
                }
            }
        }
        catch
        {
            // Non-JSON or parsing failure fallback
        }
        return dict;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
