using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Interfaces;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ICompanyContext _companyContext;

    public HomeController(
        IDashboardService dashboardService,
        ICompanyContext companyContext)
    {
        _dashboardService = dashboardService;
        _companyContext = companyContext;
    }

    [Route("")]
    [Route("Home/Index")]
    [Route("Dashboard")]
    public async Task<IActionResult> Index([FromQuery] DashboardDateFilterDto filter)
    {
        filter ??= new DashboardDateFilterDto();
        if (string.IsNullOrWhiteSpace(filter.Period))
        {
            filter.Period = "ThisMonth";
        }

        var isSuperAdmin = User.IsInRole("Super Admin");
        var activeCompId = _companyContext.CurrentCompanyId;

        // Super Admin in System Mode (no active company context) -> Show Super Admin Dashboard
        bool showSuperAdminDashboard = isSuperAdmin && (!activeCompId.HasValue || activeCompId.Value <= 0);

        var viewModel = new UnifiedDashboardViewModel
        {
            IsSuperAdminDashboard = showSuperAdminDashboard,
            IsSuperAdminUser = isSuperAdmin,
            ActiveCompanyId = activeCompId,
            ActiveCompanyName = _companyContext.CurrentCompanyName,
            ActiveCompanyCode = _companyContext.CurrentCompanyCode,
            Filter = filter
        };

        if (showSuperAdminDashboard)
        {
            viewModel.SuperAdminModel = await _dashboardService.GetSuperAdminDashboardAsync(filter, User);
        }
        else
        {
            viewModel.CompanyAdminModel = await _dashboardService.GetCompanyAdminDashboardAsync(filter, User);
        }

        return View(viewModel);
    }

    [HttpGet("api/dashboard/charts")]
    public async Task<IActionResult> GetCharts([FromQuery] DashboardDateFilterDto filter)
    {
        filter ??= new DashboardDateFilterDto();
        var chartData = await _dashboardService.GetCompanyChartsAsync(filter, User);
        return Json(new { success = true, data = chartData });
    }

    [HttpGet("api/dashboard/summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DashboardDateFilterDto filter)
    {
        filter ??= new DashboardDateFilterDto();
        var isSuperAdmin = User.IsInRole("Super Admin");
        var activeCompId = _companyContext.CurrentCompanyId;

        if (isSuperAdmin && (!activeCompId.HasValue || activeCompId.Value <= 0))
        {
            var superData = await _dashboardService.GetSuperAdminDashboardAsync(filter, User);
            return Json(new { success = true, mode = "SuperAdmin", data = superData });
        }
        else
        {
            var companyData = await _dashboardService.GetCompanyAdminDashboardAsync(filter, User);
            return Json(new { success = true, mode = "CompanyAdmin", data = companyData });
        }
    }

    [HttpGet("Help")]
    public IActionResult Help()
    {
        return View();
    }

    [HttpGet("Help/DownloadDoc")]
    public IActionResult DownloadHelpDoc()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "help", "SmartERP_User_Guide.doc");
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("User guide file not found. Please contact support.");
        }
        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, "application/msword", "SmartERP_User_Guide.doc");
    }
}
