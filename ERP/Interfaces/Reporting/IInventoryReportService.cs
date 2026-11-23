using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.ViewModels.Reporting;

namespace ERP.Interfaces.Reporting;

public interface IInventoryReportService
{
    Task<List<CurrentStockDto>> GetCurrentStockReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<CurrentStockDto>> GetLowStockReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<CurrentStockDto>> GetOutOfStockReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<StockLedgerRowDto>> GetStockLedgerAsync(int productId, int? warehouseId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<List<StockMovementDto>> GetStockMovementReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<CurrentStockDto>> GetStockValuationReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<CurrentStockDto>> GetOpeningStockReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<CurrentStockDto>> GetClosingStockReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<StockTransferReportDto>> GetStockTransferReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<StockAdjustmentReportDto>> GetStockAdjustmentReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<PhysicalVerificationReportDto>> GetPhysicalStockVerificationReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<ProductMovementAnalysisDto>> GetFastMovingProductsAsync(ReportFilterViewModel filter, int limit = 20, CancellationToken ct = default);
    Task<List<ProductMovementAnalysisDto>> GetSlowMovingProductsAsync(ReportFilterViewModel filter, int limit = 20, CancellationToken ct = default);
    Task<List<ProductMovementAnalysisDto>> GetNonMovingProductsAsync(ReportFilterViewModel filter, int days = 90, CancellationToken ct = default);
    Task<List<ReorderSuggestionDto>> GetReorderSuggestionsAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<ReportSummaryCard>> GetInventorySummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default);
}
