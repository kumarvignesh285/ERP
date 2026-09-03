using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Interfaces;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize]
[Route("Audit")]
public class AuditController : Controller
{
    private readonly ILoginHistoryService _loginHistoryService;
    private readonly IAuditService _auditService;
    private readonly IMasterService _masterService;

    public AuditController(
        ILoginHistoryService loginHistoryService,
        IAuditService auditService,
        IMasterService masterService)
    {
        _loginHistoryService = loginHistoryService;
        _auditService = auditService;
        _masterService = masterService;
    }

    [HttpGet("Logs")]
    public async Task<IActionResult> Logs([FromQuery] AuditLogFilterViewModel filter)
    {
        var isSuperAdmin = User.IsInRole("Super Admin");
        var (items, totalCount, statistics) = await _auditService.GetAuditLogsAsync(filter, User);

        var viewModel = new AuditLogPageViewModel
        {
            Logs = items,
            Filter = filter,
            TotalCount = totalCount,
            Statistics = statistics,
            IsSuperAdmin = isSuperAdmin,
            CurrentUserRole = isSuperAdmin ? "Super Admin" : (User.IsInRole("CompanyAdmin") || User.IsInRole("Admin") ? "CompanyAdmin" : "CompanyUser"),
            UserCompanyId = int.TryParse(User.FindFirst("CompanyId")?.Value, out var cid) ? cid : null,
            AvailableModules = new List<string>
            {
                "Authentication", "Company", "Security", "Settings",
                "Products", "Customers", "Suppliers", "Categories", "Brands",
                "Sales", "Purchase", "Inventory", "Accounts", "CRM", "Reports"
            },
            AvailableActions = new List<string>
            {
                "CREATE", "UPDATE", "DELETE", "VIEW", "LOGIN", "LOGOUT", "LOGIN_FAILED",
                "SECURITY_WARNING", "COMPANY_SWITCH", "PERMISSION_UPDATE", "EXPORT", "IMPORT"
            }
        };

        if (isSuperAdmin)
        {
            viewModel.AvailableCompanies = await _masterService.GetAllCompaniesAsync();
        }

        return View(viewModel);
    }

    [HttpGet("GetLogDetails/{id}")]
    public async Task<IActionResult> GetLogDetails(int id)
    {
        var log = await _auditService.GetAuditLogByIdAsync(id, User);
        if (log == null)
        {
            return Json(new { success = false, message = "Audit log not found or access denied." });
        }

        return Json(new { success = true, data = log });
    }

    [HttpGet("ExportCsv")]
    public async Task<IActionResult> ExportCsv([FromQuery] AuditLogFilterViewModel filter)
    {
        var bytes = await _auditService.ExportAuditLogsCsvAsync(filter, User);
        var fileName = $"AuditLogs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    [HttpGet("LoginHistory")]
    public async Task<IActionResult> LoginHistory([FromQuery] LoginHistoryFilterViewModel filter)
    {
        var isSuperAdmin = User.IsInRole("Super Admin");
        var (items, totalCount) = await _loginHistoryService.GetLoginHistoryAsync(filter, User);

        var viewModel = new LoginHistoryPageViewModel
        {
            Histories = items,
            Filter = filter,
            TotalCount = totalCount,
            IsSuperAdmin = isSuperAdmin,
            CurrentUserRole = isSuperAdmin ? "Super Admin" : (User.IsInRole("CompanyAdmin") || User.IsInRole("Admin") ? "CompanyAdmin" : "CompanyUser"),
            UserCompanyId = int.TryParse(User.FindFirst("CompanyId")?.Value, out var cid) ? cid : null
        };

        if (isSuperAdmin)
        {
            viewModel.AvailableCompanies = await _masterService.GetAllCompaniesAsync();
        }

        return View(viewModel);
    }

    [HttpGet("ActivityLogs")]
    [Authorize(Roles = "Super Admin")]
    public async Task<IActionResult> ActivityLogs([FromQuery] LoginHistoryFilterViewModel filter)
    {
        var (items, totalCount) = await _loginHistoryService.GetActivityLogsAsync(filter, User);

        var viewModel = new LoginHistoryPageViewModel
        {
            ActivityLogs = items,
            Filter = filter,
            TotalCount = totalCount,
            IsSuperAdmin = true,
            CurrentUserRole = "Super Admin"
        };

        return View(viewModel);
    }
}
