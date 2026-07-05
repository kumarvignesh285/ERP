using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Filters;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize]
[Permission("Accounts", "View")]
[Route("Accounts")]
public class AccountsController : Controller
{
    private readonly IAccountingService _accountingService;
    private readonly IMasterService _masterService;

    public AccountsController(IAccountingService accountingService, IMasterService masterService)
    {
        _accountingService = accountingService;
        _masterService = masterService;
    }

    private async Task<VoucherPageViewModel> BuildVoucherPageModel(string voucherType)
    {
        var ledgers = await _masterService.GetLedgersAsync();
        return new VoucherPageViewModel
        {
            Vouchers = await _accountingService.GetVouchersAsync(voucherType),
            LedgerLookups = ledgers.Select(l => new LedgerLookupViewModel
            {
                Id = l.Id,
                LedgerCode = l.LedgerCode,
                LedgerName = l.LedgerName,
                BalanceType = l.BalanceType
            }).ToList()
        };
    }

    // --- Receipt Voucher ---
    [HttpGet("ReceiptVoucher")]
    public async Task<IActionResult> ReceiptVoucher()
    {
        return View(await BuildVoucherPageModel("Receipt"));
    }

    // --- Payment Voucher ---
    [HttpGet("PaymentVoucher")]
    public async Task<IActionResult> PaymentVoucher()
    {
        return View(await BuildVoucherPageModel("Payment"));
    }

    // --- Contra Voucher ---
    [HttpGet("ContraVoucher")]
    public async Task<IActionResult> ContraVoucher()
    {
        return View(await BuildVoucherPageModel("Contra"));
    }

    // --- Journal Voucher ---
    [HttpGet("JournalVoucher")]
    public async Task<IActionResult> JournalVoucher()
    {
        return View(await BuildVoucherPageModel("Journal"));
    }

    // --- Debit Note ---
    [HttpGet("DebitNote")]
    public async Task<IActionResult> DebitNote()
    {
        return View(await BuildVoucherPageModel("DebitNote"));
    }

    // --- Credit Note ---
    [HttpGet("CreditNote")]
    public async Task<IActionResult> CreditNote()
    {
        return View(await BuildVoucherPageModel("CreditNote"));
    }

    [HttpPost("SaveVoucher")]
    public async Task<IActionResult> SaveVoucher([FromBody] Voucher voucher)
    {
        if (ModelState.IsValid)
        {
            await _accountingService.SaveVoucherAsync(voucher);
            return Json(new { success = true, message = "Voucher saved successfully." });
        }
        return Json(new { success = false, message = "Invalid data." });
    }

    [HttpPost("DeleteVoucher")]
    public async Task<IActionResult> DeleteVoucher(int id)
    {
        await _accountingService.DeleteVoucherAsync(id);
        return Json(new { success = true });
    }

    [HttpGet("GetVoucher/{id}")]
    public async Task<IActionResult> GetVoucher(int id)
    {
        var voucher = await _accountingService.GetVoucherByIdAsync(id);
        if (voucher == null) return NotFound();

        var ledgers = await _masterService.GetLedgersAsync();
        foreach (var item in voucher.Items)
        {
            var ledger = ledgers.FirstOrDefault(l => l.Id == item.LedgerId);
            if (ledger != null)
            {
                item.Ledger = ledger;
            }
        }

        return Json(voucher);
    }

    // --- Cash Book ---
    [HttpGet("CashBook")]
    public async Task<IActionResult> CashBook(DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.Today.AddMonths(-1);
        var end = endDate ?? DateTime.Today;
        return View(new BookPageViewModel
        {
            StartDate = start.ToString("yyyy-MM-dd"),
            EndDate = end.ToString("yyyy-MM-dd"),
            Items = await _accountingService.GetCashBookAsync(start, end)
        });
    }

    // --- Bank Book ---
    [HttpGet("BankBook")]
    public async Task<IActionResult> BankBook(DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.Today.AddMonths(-1);
        var end = endDate ?? DateTime.Today;
        return View(new BookPageViewModel
        {
            StartDate = start.ToString("yyyy-MM-dd"),
            EndDate = end.ToString("yyyy-MM-dd"),
            Items = await _accountingService.GetBankBookAsync(start, end)
        });
    }
}
