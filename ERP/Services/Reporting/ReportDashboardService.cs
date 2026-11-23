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
using ERP.ViewModels;
using ERP.ViewModels.Reporting;

namespace ERP.Services.Reporting;

public class ReportDashboardService : IReportDashboardService
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;
    private readonly ISalesReportService _salesReportService;
    private readonly IPurchaseReportService _purchaseReportService;
    private readonly IInventoryReportService _inventoryReportService;
    private readonly IAccountsReportService _accountsReportService;
    private readonly IAuditReportService _auditReportService;
    private readonly IBusinessAnalyticsService _biService;

    public ReportDashboardService(
        AppDbContext context,
        ICompanyContext companyContext,
        ISalesReportService salesReportService,
        IPurchaseReportService purchaseReportService,
        IInventoryReportService inventoryReportService,
        IAccountsReportService accountsReportService,
        IAuditReportService auditReportService,
        IBusinessAnalyticsService biService)
    {
        _context = context;
        _companyContext = companyContext;
        _salesReportService = salesReportService;
        _purchaseReportService = purchaseReportService;
        _inventoryReportService = inventoryReportService;
        _accountsReportService = accountsReportService;
        _auditReportService = auditReportService;
        _biService = biService;
    }

    private int? ActiveCompanyId => _companyContext.CurrentCompanyId;

    public async Task<ReportsDashboardDto> GetAdvancedDashboardDataAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);

        // Sales metrics
        var salesCards = await _salesReportService.GetSalesSummaryCardsAsync(filter, ct);
        var dailySales = await _salesReportService.GetDailySalesReportAsync(filter, ct);

        // Purchase metrics
        var purchaseCards = await _purchaseReportService.GetPurchaseSummaryCardsAsync(filter, ct);
        var dailyPurchases = await _purchaseReportService.GetDailyPurchaseReportAsync(filter, ct);

        // Outstanding
        var custOutstanding = await _accountsReportService.GetCustomerOutstandingAsync(filter, ct);
        var suppOutstanding = await _accountsReportService.GetSupplierOutstandingAsync(filter, ct);

        // Inventory
        var stockCards = await _inventoryReportService.GetInventorySummaryCardsAsync(filter, ct);
        var lowStock = await _inventoryReportService.GetLowStockReportAsync(filter, ct);
        var outStock = await _inventoryReportService.GetOutOfStockReportAsync(filter, ct);

        // Top 10 Products & Customers
        var topProducts = await _biService.GetTop10ProductsAsync(filter, ct);
        var topCustomers = (await _salesReportService.GetCustomerWiseSalesAsync(filter, ct)).Take(10).ToList();
        var categorySales = await _salesReportService.GetCategoryWiseSalesAsync(filter, ct);

        // Trends
        var salesTrend = dailySales.Select(s => new DailyTrendDto
        {
            Date = s.Date,
            DateLabel = s.DateDisplay,
            SalesAmount = s.NetSales,
            SalesInvoiceCount = s.InvoiceCount
        }).OrderBy(x => x.Date).ToList();

        var purchaseTrend = dailyPurchases.Select(p => new DailyTrendDto
        {
            Date = p.Date,
            DateLabel = p.DateDisplay,
            PurchaseAmount = p.NetPurchase,
            PurchaseInvoiceCount = p.BillCount
        }).OrderBy(x => x.Date).ToList();

        // Totals
        var totalSales = dailySales.Sum(x => x.NetSales);
        var totalPurchase = dailyPurchases.Sum(x => x.NetPurchase);
        var grossProfit = totalSales - totalPurchase;

        // Payment mode breakdown
        var salesInvoices = await _context.SalesInvoices.AsNoTracking()
            .Where(s => s.IsActive && s.Status != "Cancelled" && s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate &&
                        (ActiveCompanyId == null || s.CompanyId == ActiveCompanyId))
            .GroupBy(s => string.IsNullOrWhiteSpace(s.PaymentTerms) ? "Direct / Cash" : s.PaymentTerms)
            .Select(g => new { Mode = g.Key, Amount = g.Sum(x => x.GrandTotal) })
            .ToDictionaryAsync(x => x.Mode, x => x.Amount, ct);

        // Recent Audit Activity
        var recentActivity = await _auditReportService.GetAuditTrailReportAsync(filter, ct);

        return new ReportsDashboardDto
        {
            TotalSales = totalSales,
            TotalPurchase = totalPurchase,
            NetSales = totalSales,
            GrossProfit = grossProfit,
            IsGrossProfitReliable = totalPurchase > 0 || totalSales > 0,
            TotalOutstandingReceivable = custOutstanding.Sum(x => x.NetOutstanding),
            TotalOutstandingPayable = suppOutstanding.Sum(x => x.NetOutstanding),
            TotalProducts = lowStock.Count + outStock.Count + 10,
            LowStockCount = lowStock.Count,
            OutOfStockCount = outStock.Count,
            SalesTrend = salesTrend,
            PurchaseTrend = purchaseTrend,
            PaymentModeBreakdown = salesInvoices,
            TopProducts = topProducts,
            TopCustomers = topCustomers,
            CategorySales = categorySales,
            StockAlerts = lowStock.Concat(outStock).Take(15).ToList(),
            RecentActivity = recentActivity.Take(10).ToList()
        };
    }

    public async Task<SimpleModeDashboardDto> GetSimpleModeDashboardDataAsync(CancellationToken ct = default)
    {
        var todayFilter = new ReportFilterViewModel { DatePreset = "today" };
        var todaySalesList = await _salesReportService.GetDailySalesReportAsync(todayFilter, ct);
        var todaySale = todaySalesList.FirstOrDefault() ?? new DailySalesDto();

        var todayPurchaseList = await _purchaseReportService.GetDailyPurchaseReportAsync(todayFilter, ct);
        var todayPurchase = todayPurchaseList.FirstOrDefault() ?? new DailyPurchaseDto();

        // 7-day summary
        var weekFilter = new ReportFilterViewModel { DatePreset = "this_week" };
        var weekSales = await _salesReportService.GetDailySalesReportAsync(weekFilter, ct);

        var topProducts = await _salesReportService.GetProductWiseSalesAsync(todayFilter, ct);
        if (!topProducts.Any())
        {
            topProducts = await _salesReportService.GetProductWiseSalesAsync(weekFilter, ct);
        }

        var custOutstanding = await _accountsReportService.GetCustomerOutstandingAsync(new ReportFilterViewModel(), ct);
        var suppOutstanding = await _accountsReportService.GetSupplierOutstandingAsync(new ReportFilterViewModel(), ct);

        var lowStock = await _inventoryReportService.GetLowStockReportAsync(new ReportFilterViewModel(), ct);
        var outStock = await _inventoryReportService.GetOutOfStockReportAsync(new ReportFilterViewModel(), ct);

        decimal estimatedProfit = todaySale.NetSales - todayPurchase.NetPurchase;

        return new SimpleModeDashboardDto
        {
            TodaysSales = todaySale.NetSales,
            TodaysCollection = todaySale.PaidAmount,
            TodaysBills = todaySale.InvoiceCount,
            EstimatedGrossProfit = estimatedProfit,
            IsGrossProfitReliable = todaySale.NetSales > 0,
            LowStockCount = lowStock.Count,
            OutOfStockCount = outStock.Count,
            CustomerOutstanding = custOutstanding.Sum(x => x.NetOutstanding),
            SupplierOutstanding = suppOutstanding.Sum(x => x.NetOutstanding),
            TopSellingProducts = topProducts.Take(10).ToList(),
            DailySalesSummary = weekSales.Take(7).ToList(),
            StockAlertItems = lowStock.Concat(outStock).Take(10).ToList()
        };
    }
}
