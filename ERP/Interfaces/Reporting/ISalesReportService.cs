using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.ViewModels.Reporting;

namespace ERP.Interfaces.Reporting;

public interface ISalesReportService
{
    Task<List<DailySalesDto>> GetDailySalesReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<MonthlySalesDto>> GetMonthlySalesReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<YearlySalesDto>> GetYearlySalesReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<SalesRegisterDto>> GetSalesRegisterAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<ProductWiseSalesDto>> GetProductWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<CategoryWiseSalesDto>> GetCategoryWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<BrandWiseSalesDto>> GetBrandWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<CustomerWiseSalesDto>> GetCustomerWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<SalespersonWiseSalesDto>> GetSalespersonWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<SalesReturnReportDto>> GetSalesReturnReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<DiscountReportDto>> GetDiscountReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<PaymentCollectionReportDto>> GetPaymentCollectionReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<BillDetailsReportDto>> GetBillDetailsReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<CancelledSalesReportDto>> GetCancelledSalesReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<ReportSummaryCard>> GetSalesSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default);
}
