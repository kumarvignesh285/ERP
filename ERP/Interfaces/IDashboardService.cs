using System.Security.Claims;
using System.Threading.Tasks;
using ERP.ViewModels;

namespace ERP.Interfaces;

public interface IDashboardService
{
    Task<SuperAdminDashboardViewModel> GetSuperAdminDashboardAsync(DashboardDateFilterDto filter, ClaimsPrincipal user);
    Task<CompanyAdminDashboardViewModel> GetCompanyAdminDashboardAsync(DashboardDateFilterDto filter, ClaimsPrincipal user);
    Task<DashboardChartDataDto> GetCompanyChartsAsync(DashboardDateFilterDto filter, ClaimsPrincipal user);
}
