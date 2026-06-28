using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Interfaces;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize]
[Route("Reports")]
public class ReportsController : Controller
{
    private readonly IAccountingService _accountingService;
    private readonly ISalesService _salesService;
    private readonly IPurchaseService _purchaseService;
    private readonly IMasterService _masterService;

    public ReportsController(IAccountingService accountingService, ISalesService salesService, IPurchaseService purchaseService, IMasterService masterService)
    {
        _accountingService = accountingService;
        _salesService = salesService;
        _purchaseService = purchaseService;
        _masterService = masterService;
    }

    // --- Sales Reports ---
    [HttpGet("SalesReports")]
    public async Task<IActionResult> SalesReports()
    {
        var sales = await _salesService.GetInvoicesAsync();
        return View(sales);
    }

    // --- Purchase Reports ---
    [HttpGet("PurchaseReports")]
    public async Task<IActionResult> PurchaseReports()
    {
        var purchases = await _purchaseService.GetInvoicesAsync();
        return View(purchases);
    }

    // --- Inventory Reports ---
    [HttpGet("InventoryReports")]
    public async Task<IActionResult> InventoryReports()
    {
        var products = await _masterService.GetProductsAsync();
        return View(products);
    }

    // --- Accounting Reports ---
    [HttpGet("AccountingReports")]
    public async Task<IActionResult> AccountingReports(DateTime? startDate, DateTime? endDate, DateTime? balanceSheetDate)
    {
        var start = startDate ?? DateTime.Today.AddMonths(-1);
        var end = endDate ?? DateTime.Today;
        var bsDate = balanceSheetDate ?? DateTime.Today;

        return View(new AccountingReportsViewModel
        {
            StartDate = start.ToString("yyyy-MM-dd"),
            EndDate = end.ToString("yyyy-MM-dd"),
            BalanceSheetDate = bsDate.ToString("yyyy-MM-dd"),
            TrialBalance = await _accountingService.GetTrialBalanceAsync(),
            ProfitAndLoss = await _accountingService.GetProfitAndLossAsync(start, end),
            BalanceSheet = await _accountingService.GetBalanceSheetAsync(bsDate)
        });
    }
}
