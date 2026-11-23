using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.ViewModels.Reporting;

namespace ERP.Interfaces.Reporting;

public interface IReportDashboardService
{
    Task<ReportsDashboardDto> GetAdvancedDashboardDataAsync(ReportFilterViewModel filter, CancellationToken ct = default);
    Task<SimpleModeDashboardDto> GetSimpleModeDashboardDataAsync(CancellationToken ct = default);
}
