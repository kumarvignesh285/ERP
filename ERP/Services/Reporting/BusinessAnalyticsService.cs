using System;
using System.Collections.Generic;
using System.Globalization;
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

public class BusinessAnalyticsService : IBusinessAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;
    private readonly ISalesReportService _salesReportService;
    private readonly IPurchaseReportService _purchaseReportService;
    private readonly IInventoryReportService _inventoryReportService;

    public BusinessAnalyticsService(
        AppDbContext context,
        ICompanyContext companyContext,
        ISalesReportService salesReportService,
        IPurchaseReportService purchaseReportService,
        IInventoryReportService inventoryReportService)
    {
        _context = context;
        _companyContext = companyContext;
        _salesReportService = salesReportService;
        _purchaseReportService = purchaseReportService;
        _inventoryReportService = inventoryReportService;
    }

    private int? ActiveCompanyId => _companyContext.CurrentCompanyId;

    public async Task<List<GrowthAnalysisDto>> GetSalesGrowthAnalysisAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var monthly = await _salesReportService.GetMonthlySalesReportAsync(filter, ct);

        // Convert to growth analysis chronologically
        var chrono = monthly.OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();
        var result = new List<GrowthAnalysisDto>();
        decimal previous = 0m;

        foreach (var m in chrono)
        {
            result.Add(new GrowthAnalysisDto
            {
                PeriodLabel = m.MonthName,
                SalesAmount = m.NetSales,
                PreviousPeriodSales = previous
            });
            previous = m.NetSales;
        }

        return result;
    }

    public async Task<List<PeriodComparisonBiDto>> GetMonthOverMonthComparisonAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var curStart = new DateTime(today.Year, today.Month, 1);
        var curEnd = curStart.AddMonths(1).AddTicks(-1);

        var prevStart = curStart.AddMonths(-1);
        var prevEnd = curStart.AddTicks(-1);

        var curFilter = new ReportFilterViewModel { FromDate = curStart, ToDate = curEnd };
        var prevFilter = new ReportFilterViewModel { FromDate = prevStart, ToDate = prevEnd };

        var curSales = (await _salesReportService.GetDailySalesReportAsync(curFilter, ct)).Sum(x => x.NetSales);
        var prevSales = (await _salesReportService.GetDailySalesReportAsync(prevFilter, ct)).Sum(x => x.NetSales);

        var curPurchases = (await _purchaseReportService.GetDailyPurchaseReportAsync(curFilter, ct)).Sum(x => x.NetPurchase);
        var prevPurchases = (await _purchaseReportService.GetDailyPurchaseReportAsync(prevFilter, ct)).Sum(x => x.NetPurchase);

        return new List<PeriodComparisonBiDto>
        {
            new() { Metric = "Net Sales Revenue", CurrentPeriodValue = curSales, PreviousPeriodValue = prevSales },
            new() { Metric = "Net Purchases", CurrentPeriodValue = curPurchases, PreviousPeriodValue = prevPurchases },
            new() { Metric = "Gross Trade Margin", CurrentPeriodValue = curSales - curPurchases, PreviousPeriodValue = prevSales - prevPurchases }
        };
    }

    public async Task<List<PeriodComparisonBiDto>> GetYearOverYearComparisonAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var curYearStart = new DateTime(today.Year, 1, 1);
        var curYearEnd = curYearStart.AddYears(1).AddTicks(-1);

        var prevYearStart = curYearStart.AddYears(-1);
        var prevYearEnd = curYearStart.AddTicks(-1);

        var curFilter = new ReportFilterViewModel { FromDate = curYearStart, ToDate = curYearEnd };
        var prevFilter = new ReportFilterViewModel { FromDate = prevYearStart, ToDate = prevYearEnd };

        var curSales = (await _salesReportService.GetDailySalesReportAsync(curFilter, ct)).Sum(x => x.NetSales);
        var prevSales = (await _salesReportService.GetDailySalesReportAsync(prevFilter, ct)).Sum(x => x.NetSales);

        var curPurchases = (await _purchaseReportService.GetDailyPurchaseReportAsync(curFilter, ct)).Sum(x => x.NetPurchase);
        var prevPurchases = (await _purchaseReportService.GetDailyPurchaseReportAsync(prevFilter, ct)).Sum(x => x.NetPurchase);

        return new List<PeriodComparisonBiDto>
        {
            new() { Metric = "Annual Sales Revenue", CurrentPeriodValue = curSales, PreviousPeriodValue = prevSales },
            new() { Metric = "Annual Purchases", CurrentPeriodValue = curPurchases, PreviousPeriodValue = prevPurchases },
            new() { Metric = "Gross Trade Surplus", CurrentPeriodValue = curSales - curPurchases, PreviousPeriodValue = prevSales - prevPurchases }
        };
    }

    public async Task<List<ProductPerformanceRankDto>> GetTop10ProductsAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var productSales = await _salesReportService.GetProductWiseSalesAsync(filter, ct);

        int rank = 1;
        return productSales.Take(10).Select(p => new ProductPerformanceRankDto
        {
            Rank = rank++,
            ProductId = p.ProductId,
            ProductCode = p.ProductCode,
            ProductName = p.ProductName,
            CategoryName = p.CategoryName,
            QuantitySold = p.QuantitySold,
            Revenue = p.TotalSalesAmount,
            EstimatedProfit = p.TotalSalesAmount * 0.20m // Estimated conservative margin
        }).ToList();
    }

    public async Task<List<ProductPerformanceRankDto>> GetBottom10ProductsAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var productSales = await _salesReportService.GetProductWiseSalesAsync(filter, ct);

        int rank = 1;
        return productSales.Where(p => p.TotalSalesAmount > 0).OrderBy(p => p.TotalSalesAmount).Take(10).Select(p => new ProductPerformanceRankDto
        {
            Rank = rank++,
            ProductId = p.ProductId,
            ProductCode = p.ProductCode,
            ProductName = p.ProductName,
            CategoryName = p.CategoryName,
            QuantitySold = p.QuantitySold,
            Revenue = p.TotalSalesAmount,
            EstimatedProfit = p.TotalSalesAmount * 0.20m
        }).ToList();
    }

    public async Task<List<CategoryWiseSalesDto>> GetCategoryPerformanceAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        return await _salesReportService.GetCategoryWiseSalesAsync(filter, ct);
    }

    public async Task<List<DailyTrendDto>> GetProductSalesTrendsAsync(int? productId, ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);

        var query = _context.SalesInvoiceItems.AsNoTracking()
            .Include(i => i.SalesInvoice)
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= fromDate && i.SalesInvoice.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.SalesInvoice!.CompanyId == ActiveCompanyId.Value);

        if (productId.HasValue && productId.Value > 0)
            query = query.Where(i => i.ProductId == productId.Value);

        var grouped = await query
            .GroupBy(i => i.SalesInvoice!.InvoiceDate.Date)
            .Select(g => new DailyTrendDto
            {
                Date = g.Key,
                DateLabel = g.Key.ToString("dd-MMM"),
                SalesAmount = g.Sum(x => x.Amount),
                SalesInvoiceCount = g.Select(x => x.SalesInvoiceId).Distinct().Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        return grouped;
    }

    public async Task<List<CurrentStockDto>> GetDeadStockAnalysisAsync(int days = 90, CancellationToken ct = default)
    {
        var filter = new ReportFilterViewModel();
        var analysis = await _inventoryReportService.GetNonMovingProductsAsync(filter, days, ct);

        return analysis.Select(a => new CurrentStockDto
        {
            ProductId = a.ProductId,
            ProductCode = a.ProductCode,
            ProductName = a.ProductName,
            CategoryName = a.CategoryName,
            CurrentStock = a.CurrentStock
        }).ToList();
    }

    public async Task<List<InventoryTurnoverDto>> GetInventoryTurnoverAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);

        var query = _context.Products.AsNoTracking().Where(p => p.IsActive);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(p => p.CompanyId == ActiveCompanyId.Value);

        var products = await query.Include(p => p.Category).ToListAsync(ct);

        var salesQuery = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= fromDate && i.SalesInvoice.InvoiceDate <= toDate);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            salesQuery = salesQuery.Where(i => i.SalesInvoice!.CompanyId == ActiveCompanyId.Value);

        var cogsByProduct = await salesQuery
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.TotalQty, ct);

        var result = new List<InventoryTurnoverDto>();
        foreach (var p in products)
        {
            decimal qtySold = cogsByProduct.TryGetValue(p.Id, out var q) ? q : 0m;
            decimal cogs = qtySold * p.PurchasePrice;
            decimal avgInventory = Math.Max(1m, p.CurrentStock * p.PurchasePrice);

            result.Add(new InventoryTurnoverDto
            {
                ProductId = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                CategoryName = p.Category?.CategoryName ?? "General",
                CostOfGoodsSold = cogs,
                AverageInventoryValue = avgInventory
            });
        }

        return result.OrderByDescending(x => x.TurnoverRatio).ToList();
    }

    public async Task<List<ReorderSuggestionDto>> GetReorderSuggestionsBiAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        return await _inventoryReportService.GetReorderSuggestionsAsync(filter, ct);
    }

    public async Task<List<PurchaseVsSalesDto>> GetPurchaseVsSalesAnalysisAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var sales = await _salesReportService.GetMonthlySalesReportAsync(filter, ct);
        var purchases = await _purchaseReportService.GetMonthlyPurchaseReportAsync(filter, ct);

        var allPeriods = sales.Select(s => s.MonthName).Union(purchases.Select(p => p.MonthName)).Distinct().ToList();

        var salesDict = sales.ToDictionary(s => s.MonthName, s => s.NetSales);
        var purchaseDict = purchases.ToDictionary(p => p.MonthName, p => p.NetPurchase);

        return allPeriods.Select(period => new PurchaseVsSalesDto
        {
            Period = period,
            SalesAmount = salesDict.TryGetValue(period, out var s) ? s : 0m,
            PurchaseAmount = purchaseDict.TryGetValue(period, out var p) ? p : 0m
        }).ToList();
    }

    public async Task<List<ReportSummaryCard>> GetBiSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);

        var sales = await _salesReportService.GetSalesSummaryCardsAsync(filter, ct);
        var purchases = await _purchaseReportService.GetPurchaseSummaryCardsAsync(filter, ct);

        return new List<ReportSummaryCard>
        {
            sales.FirstOrDefault(x => x.Title.Contains("Net Sales")) ?? new() { Title = "Net Sales", Value = "₹0.00" },
            purchases.FirstOrDefault(x => x.Title.Contains("Net Purchase")) ?? new() { Title = "Net Purchase", Value = "₹0.00" },
            new() { Title = "Active Categories", Value = (await GetCategoryPerformanceAsync(filter, ct)).Count.ToString(), Icon = "fa-layer-group", ColorClass = "info" },
            new() { Title = "Stock Restock Alerts", Value = (await GetReorderSuggestionsBiAsync(filter, ct)).Count.ToString(), Icon = "fa-bell", ColorClass = "warning" }
        };
    }
}
