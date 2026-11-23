using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Helpers;
using ERP.Interfaces;
using ERP.Interfaces.Reporting;
using ERP.ViewModels.Reporting;

namespace ERP.Services.Reporting;

public class AuditReportService : IAuditReportService
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;

    public AuditReportService(AppDbContext context, ICompanyContext companyContext)
    {
        _context = context;
        _companyContext = companyContext;
    }

    private int? ActiveCompanyId => _companyContext.CurrentCompanyId;

    public async Task<List<EmployeeSalesReportDto>> GetEmployeeWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);

        var query = _context.SalesInvoices.AsNoTracking()
            .Where(s => s.IsActive && s.Status != "Cancelled" && s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(s => s.CompanyId == ActiveCompanyId.Value);

        var salesBySalesperson = await query
            .GroupBy(s => string.IsNullOrWhiteSpace(s.Salesperson) ? (s.CreatedBy ?? "Unknown") : s.Salesperson)
            .Select(g => new
            {
                Name = g.Key,
                InvoiceCount = g.Count(),
                TotalSales = g.Sum(x => x.GrandTotal)
            })
            .ToListAsync(ct);

        var employeesQuery = _context.Employees.AsNoTracking().Where(e => e.IsActive);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            employeesQuery = employeesQuery.Where(e => e.CompanyId == ActiveCompanyId.Value);

        var employees = await employeesQuery.ToListAsync(ct);
        var empDict = employees.ToDictionary(e => e.EmployeeName.ToLower().Trim(), e => e);

        var result = new List<EmployeeSalesReportDto>();
        foreach (var s in salesBySalesperson)
        {
            empDict.TryGetValue(s.Name.ToLower().Trim(), out var emp);
            result.Add(new EmployeeSalesReportDto
            {
                EmployeeId = emp?.Id ?? 0,
                EmployeeCode = emp?.EmployeeCode ?? "N/A",
                EmployeeName = emp?.EmployeeName ?? s.Name,
                Designation = emp?.Designation ?? "Salesperson",
                InvoiceCount = s.InvoiceCount,
                TotalSalesAmount = s.TotalSales
            });
        }

        return result.OrderByDescending(x => x.TotalSalesAmount).ToList();
    }

    public async Task<List<LoginHistoryReportDto>> GetLoginHistoryReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.LoginHistories.AsNoTracking()
            .Where(l => l.LoginTime >= fromDate && l.LoginTime <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(l => l.CompanyId == ActiveCompanyId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(l => l.Username.ToLower().Contains(search) || (l.IPAddress != null && l.IPAddress.Contains(search)));
        }

        return await query
            .OrderByDescending(l => l.LoginTime)
            .Take(500)
            .Select(l => new LoginHistoryReportDto
            {
                Id = l.Id,
                Username = l.Username,
                Role = l.Role,
                LoginTime = l.LoginTime,
                LogoutTime = l.LogoutTime,
                Status = l.Status,
                IPAddress = l.IPAddress,
                Browser = l.Browser,
                OperatingSystem = l.OperatingSystem,
                Device = l.Device
            })
            .ToListAsync(ct);
    }

    public async Task<List<UserActivityReportDto>> GetUserActivityReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.UserActivityLogs.AsNoTracking()
            .Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(a => a.NewCompanyId == ActiveCompanyId.Value || a.PreviousCompanyId == ActiveCompanyId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(a => a.Username.ToLower().Contains(search) || a.ActivityType.ToLower().Contains(search) || (a.Description != null && a.Description.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(500)
            .Select(a => new UserActivityReportDto
            {
                Id = a.Id,
                Username = a.Username,
                Role = a.Role,
                ActivityType = a.ActivityType,
                Description = a.Description,
                IPAddress = a.IPAddress,
                Timestamp = a.Timestamp
            })
            .ToListAsync(ct);
    }

    public async Task<List<AuditTrailReportDto>> GetAuditTrailReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.AuditLogs.AsNoTracking()
            .Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(a => a.CompanyId == ActiveCompanyId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(a => a.UserName.ToLower().Contains(search) || a.Module.ToLower().Contains(search) || a.Action.ToLower().Contains(search) || (a.Description != null && a.Description.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(500)
            .Select(a => new AuditTrailReportDto
            {
                Id = a.Id,
                UserName = a.UserName,
                Action = a.Action,
                Module = a.Module,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Description = a.Description,
                IpAddress = a.IpAddress,
                Status = a.Status,
                Severity = a.Severity,
                Timestamp = a.Timestamp
            })
            .ToListAsync(ct);
    }

    public async Task<List<AuditTrailReportDto>> GetBillModificationHistoryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.AuditLogs.AsNoTracking()
            .Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate &&
                        (a.EntityName == "SalesInvoice" || a.EntityName == "PurchaseInvoice" || a.Module == "Sales" || a.Module == "Purchase") &&
                        (a.Action == "UPDATE" || a.Action == "EDIT" || a.Action == "MODIFY"));

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(a => a.CompanyId == ActiveCompanyId.Value);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(500)
            .Select(a => new AuditTrailReportDto
            {
                Id = a.Id,
                UserName = a.UserName,
                Action = a.Action,
                Module = a.Module,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Description = a.Description,
                IpAddress = a.IpAddress,
                Status = a.Status,
                Severity = a.Severity,
                Timestamp = a.Timestamp
            })
            .ToListAsync(ct);
    }

    public async Task<List<AuditTrailReportDto>> GetDeletedTransactionHistoryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.AuditLogs.AsNoTracking()
            .Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate &&
                        (a.Action == "DELETE" || a.Action == "REMOVE"));

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(a => a.CompanyId == ActiveCompanyId.Value);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(500)
            .Select(a => new AuditTrailReportDto
            {
                Id = a.Id,
                UserName = a.UserName,
                Action = a.Action,
                Module = a.Module,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Description = a.Description,
                IpAddress = a.IpAddress,
                Status = a.Status,
                Severity = a.Severity,
                Timestamp = a.Timestamp
            })
            .ToListAsync(ct);
    }

    public async Task<List<EmployeeSalesReportDto>> GetCashierWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);

        var query = _context.SalesInvoices.AsNoTracking()
            .Where(s => s.IsActive && s.Status != "Cancelled" && s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(s => s.CompanyId == ActiveCompanyId.Value);

        return await query
            .GroupBy(s => string.IsNullOrWhiteSpace(s.CreatedBy) ? "System" : s.CreatedBy)
            .Select(g => new EmployeeSalesReportDto
            {
                EmployeeId = 0,
                EmployeeCode = "CASHIER",
                EmployeeName = g.Key,
                Designation = "Cashier / Operator",
                InvoiceCount = g.Count(),
                TotalSalesAmount = g.Sum(x => x.GrandTotal)
            })
            .OrderByDescending(x => x.TotalSalesAmount)
            .ToListAsync(ct);
    }

    public async Task<List<ReportSummaryCard>> GetAuditSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);

        var loginsQuery = _context.LoginHistories.AsNoTracking().Where(l => l.LoginTime >= fromDate && l.LoginTime <= toDate);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            loginsQuery = loginsQuery.Where(l => l.CompanyId == ActiveCompanyId.Value);

        var totalLogins = await loginsQuery.CountAsync(ct);
        var failedLogins = await loginsQuery.CountAsync(l => l.Status == "Failed", ct);

        var auditQuery = _context.AuditLogs.AsNoTracking().Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            auditQuery = auditQuery.Where(a => a.CompanyId == ActiveCompanyId.Value);

        var totalEvents = await auditQuery.CountAsync(ct);
        var deletedCount = await auditQuery.CountAsync(a => a.Action == "DELETE", ct);

        return new List<ReportSummaryCard>
        {
            new() { Title = "Total Audit Events", Value = totalEvents.ToString("N0"), Icon = "fa-shield-halved", ColorClass = "primary" },
            new() { Title = "User Logins", Value = totalLogins.ToString("N0"), Icon = "fa-right-to-bracket", ColorClass = "info" },
            new() { Title = "Failed Logins", Value = failedLogins.ToString("N0"), Icon = "fa-triangle-exclamation", ColorClass = "warning" },
            new() { Title = "Deleted Transactions", Value = deletedCount.ToString("N0"), Icon = "fa-trash-can", ColorClass = "danger" }
        };
    }
}
