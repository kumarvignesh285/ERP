using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.ViewModels.Reporting;

namespace ERP.Interfaces.Reporting;

public interface ICustomerSupplierReportService
{
    // Customer Reports
    Task<List<CustomerMasterReportDto>> GetCustomerListAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<CustomerMasterReportDto>> GetNewCustomersAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<CustomerWiseSalesDto>> GetTopCustomersAsync(ReportFilterViewModel filter, int limit = 20, CancellationToken ct = default);
    Task<List<SalesRegisterDto>> GetCustomerPurchaseHistoryAsync(int customerId, ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<OutstandingReportDto>> GetCustomerOutstandingAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<LedgerStatementRowDto>> GetCustomerLedgerAsync(int customerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<List<InactiveEntityDto>> GetInactiveCustomersAsync(ReportFilterViewModel filter, int inactiveDays = 60, CancellationToken ct = default);

    // Supplier Reports
    Task<List<SupplierMasterReportDto>> GetSupplierListAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<SupplierWisePurchaseDto>> GetTopSuppliersAsync(ReportFilterViewModel filter, int limit = 20, CancellationToken ct = default);
    Task<List<PurchaseRegisterDto>> GetSupplierPurchaseHistoryAsync(int supplierId, ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<OutstandingReportDto>> GetSupplierOutstandingAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<LedgerStatementRowDto>> GetSupplierLedgerAsync(int supplierId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<List<PurchasePaymentReportDto>> GetSupplierPaymentHistoryAsync(int supplierId, ReportFilterViewModel filter, CancellationToken ct = default);
    
    Task<List<ReportSummaryCard>> GetCustomerSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<ReportSummaryCard>> GetSupplierSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default);
}
