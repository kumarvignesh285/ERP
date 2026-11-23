using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.ViewModels.Reporting;

namespace ERP.Interfaces.Reporting;

public interface IAuditReportService
{
    Task<List<EmployeeSalesReportDto>> GetEmployeeWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<LoginHistoryReportDto>> GetLoginHistoryReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<UserActivityReportDto>> GetUserActivityReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<AuditTrailReportDto>> GetAuditTrailReportAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<AuditTrailReportDto>> GetBillModificationHistoryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<AuditTrailReportDto>> GetDeletedTransactionHistoryAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<EmployeeSalesReportDto>> GetCashierWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<List<ReportSummaryCard>> GetAuditSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default);
}
