using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.ViewModels.Reporting;

namespace ERP.Interfaces.Reporting;

public interface IAccountsReportService
{
    Task<List<OutstandingReportDto>> GetCustomerOutstandingAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<OutstandingReportDto>> GetSupplierOutstandingAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<LedgerStatementRowDto>> GetCustomerLedgerStatementAsync(int customerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<List<LedgerStatementRowDto>> GetSupplierLedgerStatementAsync(int supplierId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<List<CashBankBookRowDto>> GetCashBookAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<List<CashBankBookRowDto>> GetBankBookAsync(DateTime fromDate, DateTime toDate, int? bankLedgerId = null, CancellationToken ct = default);
    Task<List<VoucherReportDto>> GetReceiptVouchersAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<VoucherReportDto>> GetPaymentVouchersAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<VoucherReportDto>> GetJournalVouchersAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<LedgerSummaryDto>> GetLedgerSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<TrialBalanceRowDto>> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<ProfitAndLossReportDto> GetProfitAndLossAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<List<ReportSummaryCard>> GetAccountsSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default);
}
