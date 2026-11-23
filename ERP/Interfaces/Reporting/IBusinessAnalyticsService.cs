using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.ViewModels;
using ERP.ViewModels.Reporting;

namespace ERP.Interfaces.Reporting;

public interface IBusinessAnalyticsService
{
    Task<List<GrowthAnalysisDto>> GetSalesGrowthAnalysisAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<PeriodComparisonBiDto>> GetMonthOverMonthComparisonAsync(CancellationToken ct = default);
    Task<List<PeriodComparisonBiDto>> GetYearOverYearComparisonAsync(CancellationToken ct = default);
    Task<List<ProductPerformanceRankDto>> GetTop10ProductsAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<ProductPerformanceRankDto>> GetBottom10ProductsAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<CategoryWiseSalesDto>> GetCategoryPerformanceAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<DailyTrendDto>> GetProductSalesTrendsAsync(int? productId, ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<CurrentStockDto>> GetDeadStockAnalysisAsync(int days = 90, CancellationToken ct = default);
    Task<List<InventoryTurnoverDto>> GetInventoryTurnoverAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<ReorderSuggestionDto>> GetReorderSuggestionsBiAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<PurchaseVsSalesDto>> GetPurchaseVsSalesAnalysisAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<ReportSummaryCard>> GetBiSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default);
}
