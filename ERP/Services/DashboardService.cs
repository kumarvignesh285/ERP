using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPermissionService _permissionService;

    public DashboardService(
        AppDbContext context,
        ICompanyContext companyContext,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IPermissionService permissionService)
    {
        _context = context;
        _companyContext = companyContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _permissionService = permissionService;
    }

    public async Task<SuperAdminDashboardViewModel> GetSuperAdminDashboardAsync(DashboardDateFilterDto filter, ClaimsPrincipal user)
    {
        var today = DateTime.UtcNow.Date;
        var todayEnd = today.AddDays(1).AddTicks(-1);

        var companies = await _context.Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var totalCompanies = companies.Count;
        var activeCompanies = companies.Count(c => c.IsActive);
        var inactiveCompanies = totalCompanies - activeCompanies;

        var users = await _userManager.Users.AsNoTracking().ToListAsync();
        var totalUsers = users.Count;
        var activeUsers = users.Count(u => u.IsActive);

        var totalRoles = await _roleManager.Roles.CountAsync();

        // System-wide product, customer, supplier counts
        var totalProducts = await _context.Products.IgnoreQueryFilters().CountAsync(p => p.IsActive);
        var totalCustomers = await _context.Customers.IgnoreQueryFilters().CountAsync(c => c.IsActive);
        var totalSuppliers = await _context.Suppliers.IgnoreQueryFilters().CountAsync(s => s.IsActive);

        // System-wide sales & purchases totals
        var totalSales = await _context.SalesInvoices.IgnoreQueryFilters().Where(s => s.IsActive).SumAsync(s => (decimal?)s.GrandTotal) ?? 0m;
        var totalPurchases = await _context.PurchaseInvoices.IgnoreQueryFilters().Where(p => p.IsActive).SumAsync(p => (decimal?)p.GrandTotal) ?? 0m;

        // Security / Login counts today
        var successfulLogins = await _context.LoginHistories
            .IgnoreQueryFilters()
            .CountAsync(l => l.LoginTime >= today && l.LoginTime <= todayEnd && l.Status == "Success");

        var failedLogins = await _context.LoginHistories
            .IgnoreQueryFilters()
            .CountAsync(l => l.LoginTime >= today && l.LoginTime <= todayEnd && l.Status == "Failed");

        var activeSessions = await _context.LoginHistories
            .IgnoreQueryFilters()
            .CountAsync(l => l.LogoutTime == null && l.Status == "Success");

        // Company summary rows with per-company user counts and sales totals
        var companyUsersGroup = users
            .Where(u => u.CompanyId.HasValue)
            .GroupBy(u => u.CompanyId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var salesByCompany = await _context.SalesInvoices
            .IgnoreQueryFilters()
            .Where(s => s.IsActive)
            .GroupBy(s => s.CompanyId)
            .Select(g => new { CompanyId = g.Key, Total = g.Sum(s => s.GrandTotal) })
            .ToDictionaryAsync(g => g.CompanyId, g => g.Total);

        var purchasesByCompany = await _context.PurchaseInvoices
            .IgnoreQueryFilters()
            .Where(p => p.IsActive)
            .GroupBy(p => p.CompanyId)
            .Select(g => new { CompanyId = g.Key, Total = g.Sum(p => p.GrandTotal) })
            .ToDictionaryAsync(g => g.CompanyId, g => g.Total);

        var companyRows = companies.Select(c => new CompanySummaryRowDto
        {
            CompanyId = c.Id,
            CompanyCode = c.CompanyCode,
            CompanyName = c.CompanyName,
            City = c.City ?? "N/A",
            IsActive = c.IsActive,
            UserCount = companyUsersGroup.GetValueOrDefault(c.Id, 0),
            TotalSales = salesByCompany.GetValueOrDefault(c.Id, 0m),
            TotalPurchases = purchasesByCompany.GetValueOrDefault(c.Id, 0m),
            CreatedAt = c.CreatedAt
        }).ToList();

        // Recent System-wide Audit Activities
        var recentAuditLogs = await _context.AuditLogs
            .IgnoreQueryFilters()
            .Include(a => a.Company)
            .OrderByDescending(a => a.Timestamp)
            .Take(6)
            .Select(a => new RecentActivityDto
            {
                Id = a.Id,
                Action = a.Action,
                Module = a.Module,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Description = a.Description ?? $"{a.Action} on {a.Module}",
                UserName = a.UserName,
                CompanyCode = a.Company != null ? a.Company.CompanyCode : (a.CompanyId.HasValue ? $"#{a.CompanyId}" : "System"),
                Severity = a.Severity,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        return new SuperAdminDashboardViewModel
        {
            TotalCompanies = totalCompanies,
            ActiveCompanies = activeCompanies,
            InactiveCompanies = inactiveCompanies,
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalRoles = totalRoles,
            SuccessfulLoginsToday = successfulLogins,
            FailedLoginsToday = failedLogins,
            ActiveSessions = activeSessions,
            TotalProducts = totalProducts,
            TotalCustomers = totalCustomers,
            TotalSuppliers = totalSuppliers,
            TotalPlatformSales = totalSales,
            TotalPlatformPurchases = totalPurchases,
            Companies = companyRows,
            RecentActivities = recentAuditLogs
        };
    }

    public async Task<CompanyAdminDashboardViewModel> GetCompanyAdminDashboardAsync(DashboardDateFilterDto filter, ClaimsPrincipal user)
    {
        var (fromDate, toDate) = ResolveDateRange(filter);
        var today = DateTime.UtcNow.Date;
        var todayEnd = today.AddDays(1).AddTicks(-1);

        int activeCompId = 0;
        string activeCompCode = "DEFAULT";
        string activeCompName = "Active Company";

        if (_companyContext.CurrentCompanyId.HasValue && _companyContext.CurrentCompanyId.Value > 0)
        {
            activeCompId = _companyContext.CurrentCompanyId.Value;
            activeCompCode = _companyContext.CurrentCompanyCode ?? "COMP";
            activeCompName = _companyContext.CurrentCompanyName ?? "Company";
        }
        else
        {
            var userClaimCompId = user.FindFirst("CompanyId")?.Value;
            if (int.TryParse(userClaimCompId, out var cid) && cid > 0)
            {
                activeCompId = cid;
                var comp = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == cid);
                if (comp != null)
                {
                    activeCompCode = comp.CompanyCode;
                    activeCompName = comp.CompanyName;
                }
            }
        }

        // Query Scopes strictly tied to Active Company ID
        var salesQuery = _context.SalesInvoices.IgnoreQueryFilters().Where(s => s.CompanyId == activeCompId && s.IsActive);
        var purchaseQuery = _context.PurchaseInvoices.IgnoreQueryFilters().Where(p => p.CompanyId == activeCompId && p.IsActive);
        var productQuery = _context.Products.IgnoreQueryFilters().Where(p => p.CompanyId == activeCompId && p.IsActive);
        var customerQuery = _context.Customers.IgnoreQueryFilters().Where(c => c.CompanyId == activeCompId && c.IsActive);
        var supplierQuery = _context.Suppliers.IgnoreQueryFilters().Where(s => s.CompanyId == activeCompId && s.IsActive);
        var employeeQuery = _context.Employees.IgnoreQueryFilters().Where(e => e.CompanyId == activeCompId && e.IsActive);

        // Sales Aggregations
        var todaySales = await salesQuery
            .Where(s => s.InvoiceDate >= today && s.InvoiceDate <= todayEnd)
            .SumAsync(s => (decimal?)s.GrandTotal) ?? 0m;

        var periodSalesQuery = salesQuery.Where(s => s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate);
        var periodSales = await periodSalesQuery.SumAsync(s => (decimal?)s.GrandTotal) ?? 0m;
        var periodSalesCount = await periodSalesQuery.CountAsync();

        var totalReceivables = await salesQuery.SumAsync(s => (decimal?)s.BalanceAmount) ?? 0m;

        // Purchase Aggregations
        var todayPurchases = await purchaseQuery
            .Where(p => p.InvoiceDate >= today && p.InvoiceDate <= todayEnd)
            .SumAsync(p => (decimal?)p.GrandTotal) ?? 0m;

        var periodPurchasesQuery = purchaseQuery.Where(p => p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate);
        var periodPurchases = await periodPurchasesQuery.SumAsync(p => (decimal?)p.GrandTotal) ?? 0m;
        var periodPurchasesCount = await periodPurchasesQuery.CountAsync();

        var totalPayables = await purchaseQuery.SumAsync(p => (decimal?)p.BalanceAmount) ?? 0m;

        // Expense Aggregations (from Payment Vouchers or Vouchers if available)
        var todayExpenses = await _context.Vouchers.IgnoreQueryFilters()
            .Where(v => v.CompanyId == activeCompId && v.IsActive && v.VoucherType == "Payment" && v.VoucherDate >= today && v.VoucherDate <= todayEnd)
            .SumAsync(v => (decimal?)v.TotalAmount) ?? 0m;

        var periodExpenses = await _context.Vouchers.IgnoreQueryFilters()
            .Where(v => v.CompanyId == activeCompId && v.IsActive && v.VoucherType == "Payment" && v.VoucherDate >= fromDate && v.VoucherDate <= toDate)
            .SumAsync(v => (decimal?)v.TotalAmount) ?? 0m;

        // Financial summary
        var grossProfit = periodSales - periodPurchases - periodExpenses;
        var marginPct = periodSales > 0 ? Math.Round((grossProfit / periodSales) * 100m, 1) : 0m;

        // Master entity metrics
        var totalCustomers = await customerQuery.CountAsync();
        var activeCustomers = totalCustomers;
        var totalSuppliers = await supplierQuery.CountAsync();
        var activeSuppliers = totalSuppliers;

        var totalProducts = await productQuery.CountAsync();
        var activeProducts = totalProducts;
        var lowStockCount = await productQuery.CountAsync(p => p.CurrentStock <= p.ReorderLevel);
        var outOfStockCount = await productQuery.CountAsync(p => p.CurrentStock <= 0);
        var stockValue = await productQuery.SumAsync(p => (decimal?)(p.CurrentStock * p.PurchasePrice)) ?? 0m;
        var totalEmployees = await employeeQuery.CountAsync();

        // Orders & Approvals
        var openSalesOrders = await _context.SalesOrders.IgnoreQueryFilters()
            .CountAsync(o => o.CompanyId == activeCompId && o.IsActive && o.Status != "Completed" && o.Status != "Cancelled");

        var openPurchaseOrders = await _context.PurchaseOrders.IgnoreQueryFilters()
            .CountAsync(o => o.CompanyId == activeCompId && o.IsActive && o.Status != "Completed" && o.Status != "Cancelled");

        var pendingApprovals = await _context.SalesOrders.IgnoreQueryFilters()
            .CountAsync(o => o.CompanyId == activeCompId && o.IsActive && o.Status == "Pending");

        // Login history for this company
        var successfulLogins = await _context.LoginHistories.IgnoreQueryFilters()
            .CountAsync(l => l.CompanyId == activeCompId && l.LoginTime >= today && l.LoginTime <= todayEnd && l.Status == "Success");

        var failedLogins = await _context.LoginHistories.IgnoreQueryFilters()
            .CountAsync(l => l.CompanyId == activeCompId && l.LoginTime >= today && l.LoginTime <= todayEnd && l.Status == "Failed");

        var activeSessions = await _context.LoginHistories.IgnoreQueryFilters()
            .CountAsync(l => l.CompanyId == activeCompId && l.LogoutTime == null && l.Status == "Success");

        // Recent Invoices & Purchases (Top 5)
        var recentSales = await salesQuery
            .Include(s => s.Customer)
            .OrderByDescending(s => s.InvoiceDate)
            .ThenByDescending(s => s.Id)
            .Take(5)
            .Select(s => new RecentSalesInvoiceDto
            {
                Id = s.Id,
                InvoiceNo = s.InvoiceNumber,
                CustomerName = s.Customer != null ? s.Customer.CustomerName : "Walk-in Customer",
                InvoiceDate = s.InvoiceDate,
                GrandTotal = s.GrandTotal,
                PaidAmount = s.PaidAmount,
                BalanceAmount = s.BalanceAmount,
                Status = s.Status
            })
            .ToListAsync();

        var recentPurchases = await purchaseQuery
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.InvoiceDate)
            .ThenByDescending(p => p.Id)
            .Take(5)
            .Select(p => new RecentPurchaseInvoiceDto
            {
                Id = p.Id,
                InvoiceNo = p.InvoiceNumber,
                SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "Vendor",
                InvoiceDate = p.InvoiceDate,
                GrandTotal = p.GrandTotal,
                PaidAmount = p.PaidAmount,
                BalanceAmount = p.BalanceAmount,
                Status = p.Status
            })
            .ToListAsync();

        // Low stock items (Top 5)
        var lowStockProducts = await productQuery
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Where(p => p.CurrentStock <= p.ReorderLevel)
            .OrderBy(p => p.CurrentStock)
            .Take(5)
            .Select(p => new LowStockProductDto
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                CategoryName = p.Category != null ? p.Category.CategoryName : "General",
                CurrentStock = p.CurrentStock,
                ReorderLevel = p.ReorderLevel,
                MinimumStock = p.MinimumStock,
                UnitName = p.Unit != null ? p.Unit.UnitName : "Pcs"
            })
            .ToListAsync();

        // Tenant-scoped Recent Activity
        var recentActivities = await _context.AuditLogs.IgnoreQueryFilters()
            .Where(a => a.CompanyId == activeCompId)
            .OrderByDescending(a => a.Timestamp)
            .Take(5)
            .Select(a => new RecentActivityDto
            {
                Id = a.Id,
                Action = a.Action,
                Module = a.Module,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Description = a.Description ?? $"{a.Action} on {a.Module}",
                UserName = a.UserName,
                Severity = a.Severity,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        // Chart Data for 30-day Trend and Category Distribution
        var chartData = await GetCompanyChartsInternalAsync(activeCompId, fromDate, toDate);

        // Permission flags
        var canViewSales = await _permissionService.HasPermissionAsync(user, "Sales Invoice", "View");
        var canCreateSales = await _permissionService.HasPermissionAsync(user, "Sales Invoice", "Edit");
        var canViewPurchases = await _permissionService.HasPermissionAsync(user, "Purchase Invoice", "View");
        var canCreatePurchases = await _permissionService.HasPermissionAsync(user, "Purchase Invoice", "Edit");
        var canViewInventory = await _permissionService.HasPermissionAsync(user, "Stock Opening", "View");
        var canViewAccounts = await _permissionService.HasPermissionAsync(user, "Receipt Voucher", "View");
        var canViewReports = await _permissionService.HasPermissionAsync(user, "Sales Reports", "View");
        var canViewAudit = user.IsInRole("Super Admin") || user.IsInRole("CompanyAdmin") || user.IsInRole("Admin");

        return new CompanyAdminDashboardViewModel
        {
            CompanyId = activeCompId,
            CompanyCode = activeCompCode,
            CompanyName = activeCompName,
            Filter = filter,
            FilterFrom = fromDate,
            FilterTo = toDate,
            TodaySales = todaySales,
            PeriodSales = periodSales,
            PeriodSalesCount = periodSalesCount,
            TotalReceivables = totalReceivables,
            TodayPurchases = todayPurchases,
            PeriodPurchases = periodPurchases,
            PeriodPurchasesCount = periodPurchasesCount,
            TotalPayables = totalPayables,
            TodayExpenses = todayExpenses,
            PeriodExpenses = periodExpenses,
            Revenue = periodSales,
            PurchasesCost = periodPurchases,
            TotalExpensesSum = periodExpenses,
            GrossProfit = grossProfit,
            NetMarginPercentage = marginPct,
            TotalCustomers = totalCustomers,
            ActiveCustomers = activeCustomers,
            TotalSuppliers = totalSuppliers,
            ActiveSuppliers = activeSuppliers,
            TotalProducts = totalProducts,
            ActiveProducts = activeProducts,
            LowStockCount = lowStockCount,
            OutOfStockCount = outOfStockCount,
            AvailableStockValue = stockValue,
            TotalEmployees = totalEmployees,
            OpenSalesOrders = openSalesOrders,
            OpenPurchaseOrders = openPurchaseOrders,
            PendingApprovalsCount = pendingApprovals,
            SuccessfulLoginsToday = successfulLogins,
            FailedLoginsToday = failedLogins,
            ActiveSessions = activeSessions,
            RecentSales = recentSales,
            RecentPurchases = recentPurchases,
            LowStockProducts = lowStockProducts,
            RecentActivities = recentActivities,
            Charts = chartData,
            CanViewSales = canViewSales,
            CanCreateSales = canCreateSales,
            CanViewPurchases = canViewPurchases,
            CanCreatePurchases = canCreatePurchases,
            CanViewInventory = canViewInventory,
            CanViewAccounts = canViewAccounts,
            CanViewReports = canViewReports,
            CanViewAudit = canViewAudit
        };
    }

    public async Task<DashboardChartDataDto> GetCompanyChartsAsync(DashboardDateFilterDto filter, ClaimsPrincipal user)
    {
        var (fromDate, toDate) = ResolveDateRange(filter);
        int activeCompId = _companyContext.CurrentCompanyId ?? 0;
        if (activeCompId == 0)
        {
            var userClaimCompId = user.FindFirst("CompanyId")?.Value;
            int.TryParse(userClaimCompId, out activeCompId);
        }

        return await GetCompanyChartsInternalAsync(activeCompId, fromDate, toDate);
    }

    private async Task<DashboardChartDataDto> GetCompanyChartsInternalAsync(int companyId, DateTime fromDate, DateTime toDate)
    {
        var dto = new DashboardChartDataDto();

        // 30 Days or Filter Period Daily Points (Max 30 points)
        var totalDays = (int)(toDate.Date - fromDate.Date).TotalDays + 1;
        if (totalDays > 30)
        {
            fromDate = toDate.Date.AddDays(-29);
            totalDays = 30;
        }
        else if (totalDays <= 0)
        {
            totalDays = 1;
        }

        var dailySales = await _context.SalesInvoices.IgnoreQueryFilters()
            .Where(s => s.CompanyId == companyId && s.IsActive && s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate)
            .GroupBy(s => s.InvoiceDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(s => s.GrandTotal) })
            .ToDictionaryAsync(g => g.Date, g => g.Total);

        var dailyPurchases = await _context.PurchaseInvoices.IgnoreQueryFilters()
            .Where(p => p.CompanyId == companyId && p.IsActive && p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate)
            .GroupBy(p => p.InvoiceDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(p => p.GrandTotal) })
            .ToDictionaryAsync(g => g.Date, g => g.Total);

        for (int i = 0; i < totalDays; i++)
        {
            var curDate = fromDate.Date.AddDays(i);
            dto.TrendLabels.Add(curDate.ToString("dd MMM"));
            dto.SalesTrendValues.Add(dailySales.GetValueOrDefault(curDate, 0m));
            dto.PurchaseTrendValues.Add(dailyPurchases.GetValueOrDefault(curDate, 0m));
        }

        // Top 5 Product Categories Stock Valuation Distribution
        var categoryStock = await _context.Products.IgnoreQueryFilters()
            .Where(p => p.CompanyId == companyId && p.IsActive)
            .Include(p => p.Category)
            .GroupBy(p => p.Category != null ? p.Category.CategoryName : "Uncategorized")
            .Select(g => new { Category = g.Key, TotalValue = g.Sum(p => p.CurrentStock * p.PurchasePrice) })
            .OrderByDescending(g => g.TotalValue)
            .Take(5)
            .ToListAsync();

        foreach (var cs in categoryStock)
        {
            dto.CategoryLabels.Add(cs.Category);
            dto.CategoryStockValues.Add(cs.TotalValue);
        }

        if (!dto.CategoryLabels.Any())
        {
            dto.CategoryLabels.Add("General");
            dto.CategoryStockValues.Add(0m);
        }

        return dto;
    }

    private static (DateTime FromDate, DateTime ToDate) ResolveDateRange(DashboardDateFilterDto filter)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        if (filter.Period?.Equals("Custom", StringComparison.OrdinalIgnoreCase) == true && filter.DateFrom.HasValue && filter.DateTo.HasValue)
        {
            return (filter.DateFrom.Value.Date, filter.DateTo.Value.Date.AddDays(1).AddTicks(-1));
        }

        return filter.Period?.ToLowerInvariant() switch
        {
            "today" => (today, today.AddDays(1).AddTicks(-1)),
            "yesterday" => (today.AddDays(-1), today.AddTicks(-1)),
            "thisweek" => (today.AddDays(-(int)today.DayOfWeek), today.AddDays(1).AddTicks(-1)),
            "lastmonth" => (
                new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                new DateTime(today.Year, today.Month, 1).AddTicks(-1)
            ),
            "thisyear" => (new DateTime(today.Year, 1, 1), today.AddDays(1).AddTicks(-1)),
            _ => (new DateTime(today.Year, today.Month, 1), today.AddDays(1).AddTicks(-1)) // Default: ThisMonth
        };
    }
}
