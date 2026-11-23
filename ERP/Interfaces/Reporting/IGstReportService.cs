using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.ViewModels.Reporting;

namespace ERP.Interfaces.Reporting;

public interface IGstReportService
{
    Task<List<GstSalesSummaryDto>> GetGstSalesSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<GstPurchaseSummaryDto>> GetGstPurchaseSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<HsnSummaryDto>> GetHsnWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<HsnSummaryDto>> GetHsnWisePurchaseAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<TaxBreakdownDto>> GetCgstSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<TaxBreakdownDto>> GetSgstSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<TaxBreakdownDto>> GetIgstSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<TaxRateSummaryDto>> GetTaxRateWiseSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<SalesReturnReportDto>> GetSalesReturnTaxSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<PurchaseReturnReportDto>> GetPurchaseReturnTaxSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<ReportSummaryCard>> GetGstSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default);
}
