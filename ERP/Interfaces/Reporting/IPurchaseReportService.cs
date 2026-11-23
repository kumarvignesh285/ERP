using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.ViewModels.Reporting;

namespace ERP.Interfaces.Reporting;

public interface IPurchaseReportService
{
    Task<List<DailyPurchaseDto>> GetDailyPurchaseReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<MonthlyPurchaseDto>> GetMonthlyPurchaseReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<PurchaseRegisterDto>> GetPurchaseRegisterAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<SupplierWisePurchaseDto>> GetSupplierWisePurchaseAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<ProductWisePurchaseDto>> GetProductWisePurchaseAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<PurchaseReturnReportDto>> GetPurchaseReturnReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<PurchaseOrderReportDto>> GetPurchaseOrderReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<PurchaseOrderReportDto>> GetPendingPurchaseOrdersAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<GoodsReceiptReportDto>> GetGoodsReceiptReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<PurchasePaymentReportDto>> GetPurchasePaymentReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<PurchasePriceHistoryDto>> GetPurchasePriceHistoryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<PurchaseTaxSummaryDto>> GetPurchaseTaxSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<ReportSummaryCard>> GetPurchaseSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default);
}
