using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.ViewModels;

namespace ERP.Interfaces;

public interface IERPReportService
{
    // Company resolution & verification
    int GetActiveCompanyId();
    string GetActiveCompanyName();

    // Sales Reporting
    Task<SalesSummaryResultDto> GetSalesSummaryAsync(DateTime fromDate, DateTime toDate, int? customerId = null, int? categoryId = null, int? brandId = null, int? productId = null, CancellationToken ct = default);
    Task<List<SalesByEntityDto>> GetSalesByCustomerAsync(DateTime fromDate, DateTime toDate, int limit = 10, CancellationToken ct = default);
    Task<List<SalesByEntityDto>> GetSalesByProductAsync(DateTime fromDate, DateTime toDate, int limit = 10, bool ascending = false, CancellationToken ct = default);
    Task<List<SalesByEntityDto>> GetSalesByCategoryAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<List<SalesByEntityDto>> GetSalesByBrandAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<PeriodComparisonResultDto> GetSalesComparisonAsync(DateTime currentFrom, DateTime currentTo, DateTime prevFrom, DateTime prevTo, CancellationToken ct = default);
    Task<List<UnpaidInvoiceDto>> GetUnpaidSalesInvoicesAsync(int? customerId = null, int limit = 20, CancellationToken ct = default);
    Task<List<OutstandingBalanceDto>> GetCustomerOutstandingBalancesAsync(int limit = 20, CancellationToken ct = default);
    Task<List<UnpaidInvoiceDto>> GetCustomerSalesHistoryAsync(int customerId, int limit = 20, CancellationToken ct = default);

    // Purchase Reporting
    Task<PurchaseSummaryResultDto> GetPurchasesSummaryAsync(DateTime fromDate, DateTime toDate, int? supplierId = null, int? categoryId = null, int? brandId = null, int? productId = null, CancellationToken ct = default);
    Task<List<PurchaseByEntityDto>> GetPurchasesBySupplierAsync(DateTime fromDate, DateTime toDate, int limit = 10, CancellationToken ct = default);
    Task<List<PurchaseByEntityDto>> GetPurchasesByProductAsync(DateTime fromDate, DateTime toDate, int limit = 10, CancellationToken ct = default);
    Task<List<PurchaseByEntityDto>> GetPurchasesByCategoryAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<List<PurchaseByEntityDto>> GetPurchasesByBrandAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<PeriodComparisonResultDto> GetPurchasesComparisonAsync(DateTime currentFrom, DateTime currentTo, DateTime prevFrom, DateTime prevTo, CancellationToken ct = default);
    Task<List<UnpaidInvoiceDto>> GetOutstandingSupplierPaymentsAsync(int? supplierId = null, int limit = 20, CancellationToken ct = default);
    Task<List<OutstandingBalanceDto>> GetSupplierOutstandingBalancesAsync(int limit = 20, CancellationToken ct = default);
    Task<List<UnpaidInvoiceDto>> GetSupplierPurchaseHistoryAsync(int supplierId, int limit = 20, CancellationToken ct = default);

    // Inventory Reporting
    Task<InventorySummaryResultDto> GetStockSummaryAsync(int? categoryId = null, int? brandId = null, int? warehouseId = null, CancellationToken ct = default);
    Task<List<ProductStockItemDto>> GetLowAndOutOfStockProductsAsync(bool outOfStockOnly = false, int limit = 50, CancellationToken ct = default);
    Task<List<ProductStockItemDto>> GetStockValuationByCategoryAsync(CancellationToken ct = default);
    Task<List<ProductStockItemDto>> GetStockValuationByBrandAsync(CancellationToken ct = default);
    Task<List<ProductStockItemDto>> GetStockValuationByWarehouseAsync(CancellationToken ct = default);
    Task<List<ProductStockItemDto>> GetProductsWithoutSalesAsync(int days = 90, int limit = 50, CancellationToken ct = default);
    Task<List<ProductStockItemDto>> GetNegativeStockProductsAsync(CancellationToken ct = default);
    Task<List<ProductStockItemDto>> GetReorderSuggestionsAsync(int limit = 50, CancellationToken ct = default);
    Task<List<DailyTrendDto>> GetProductStockMovementHistoryAsync(int productId, int limit = 20, CancellationToken ct = default);

    // Aging Analysis
    Task<AgingReportResultDto> GetReceivablesAgingAsync(CancellationToken ct = default);
    Task<AgingReportResultDto> GetPayablesAgingAsync(CancellationToken ct = default);

    // Executive Business Intelligence
    Task<ExecutiveSnapshotDto> GetExecutiveBusinessSnapshotAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<List<DailyTrendDto>> GetDailySalesAndPurchasesTrendAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<List<PeriodComparisonItemDto>> GetDecliningSalesProductsAsync(int days = 30, int limit = 10, CancellationToken ct = default);
}
