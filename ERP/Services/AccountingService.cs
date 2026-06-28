using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;

namespace ERP.Services;

public class AccountingService : IAccountingService
{
    private readonly AppDbContext _context;

    public AccountingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Voucher>> GetVouchersAsync(string? type = null)
    {
        var query = _context.Vouchers.Where(v => v.IsActive);
        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(v => v.VoucherType == type);
        }
        return await query.OrderByDescending(v => v.VoucherDate).ToListAsync();
    }

    public async Task<Voucher?> GetVoucherByIdAsync(int id)
    {
        return await _context.Vouchers
            .Include(v => v.Items)
            .ThenInclude(i => i.Ledger)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Voucher> SaveVoucherAsync(Voucher voucher)
    {
        if (voucher.Id == 0)
        {
            _context.Vouchers.Add(voucher);
        }
        else
        {
            var existing = await _context.Vouchers.Include(v => v.Items).FirstOrDefaultAsync(v => v.Id == voucher.Id);
            if (existing != null)
            {
                _context.VoucherItems.RemoveRange(existing.Items);
                _context.Entry(existing).CurrentValues.SetValues(voucher);
                foreach (var item in voucher.Items)
                {
                    existing.Items.Add(item);
                }
                voucher = existing;
            }
        }
        await _context.SaveChangesAsync();
        return voucher;
    }

    public async Task DeleteVoucherAsync(int id)
    {
        var item = await _context.Vouchers.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<LedgerBalance>> GetTrialBalanceAsync()
    {
        var ledgers = await _context.Ledgers.Include(l => l.AccountGroup).Where(l => l.IsActive).ToListAsync();
        var voucherItems = await _context.VoucherItems.Include(vi => vi.Voucher).Where(vi => vi.Voucher!.IsActive).ToListAsync();

        var result = new List<LedgerBalance>();
        foreach (var l in ledgers)
        {
            var debit = voucherItems.Where(vi => vi.LedgerId == l.Id).Sum(vi => vi.DebitAmount);
            var credit = voucherItems.Where(vi => vi.LedgerId == l.Id).Sum(vi => vi.CreditAmount);

            decimal bal = l.OpeningBalance;
            if (l.BalanceType == "Dr")
                bal += (debit - credit);
            else
                bal += (credit - debit);

            result.Add(new LedgerBalance
            {
                LedgerId = l.Id,
                LedgerCode = l.LedgerCode,
                LedgerName = l.LedgerName,
                GroupName = l.AccountGroup?.GroupName ?? "Unknown",
                Debit = debit,
                Credit = credit,
                Balance = Math.Abs(bal),
                BalanceType = bal >= 0 ? l.BalanceType : (l.BalanceType == "Dr" ? "Cr" : "Dr")
            });
        }
        return result;
    }

    public async Task<ProfitAndLossStatement> GetProfitAndLossAsync(DateTime startDate, DateTime endDate)
    {
        var balances = await GetTrialBalanceAsync();
        var pnl = new ProfitAndLossStatement();

        var salesLedgers = balances.Where(b => b.GroupName.Contains("Sales") || b.GroupName.Contains("Revenue"));
        pnl.GrossSales = salesLedgers.Sum(s => s.Credit - s.Debit);

        var purchaseLedgers = balances.Where(b => b.GroupName.Contains("Purchase") || b.GroupName.Contains("Direct Expense"));
        pnl.CostOfGoodsSold = purchaseLedgers.Sum(p => p.Debit - p.Credit);

        pnl.IndirectExpenses = balances.Where(b => b.GroupName.Contains("Indirect Expense") || b.GroupName.Contains("Administrative") || b.GroupName.Contains("Operating Expense")).ToList();
        pnl.TotalIndirectExpenses = pnl.IndirectExpenses.Sum(e => e.Balance);

        pnl.IndirectIncomes = balances.Where(b => b.GroupName.Contains("Indirect Income") || b.GroupName.Contains("Other Income")).ToList();
        pnl.TotalIndirectIncomes = pnl.IndirectIncomes.Sum(i => i.Balance);

        return pnl;
    }

    public async Task<BalanceSheetStatement> GetBalanceSheetAsync(DateTime date)
    {
        var balances = await GetTrialBalanceAsync();
        var bs = new BalanceSheetStatement();

        bs.Assets = balances.Where(b => b.GroupName.Contains("Asset") || b.GroupName.Contains("Debtors") || b.GroupName.Contains("Bank") || b.GroupName.Contains("Cash")).ToList();
        bs.TotalAssets = bs.Assets.Sum(a => a.Balance);

        bs.Liabilities = balances.Where(b => b.GroupName.Contains("Liability") || b.GroupName.Contains("Creditors") || b.GroupName.Contains("Loan")).ToList();
        bs.TotalLiabilities = bs.Liabilities.Sum(l => l.Balance);

        bs.Equities = balances.Where(b => b.GroupName.Contains("Capital") || b.GroupName.Contains("Equity") || b.GroupName.Contains("Reserves")).ToList();
        bs.TotalEquities = bs.Equities.Sum(e => e.Balance);

        return bs;
    }

    public async Task<List<VoucherItem>> GetCashBookAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.VoucherItems
            .Include(vi => vi.Voucher)
            .Include(vi => vi.Ledger)
            .Where(vi => vi.Voucher!.IsActive && vi.Ledger!.LedgerName.Contains("Cash") && vi.Voucher.VoucherDate >= startDate && vi.Voucher.VoucherDate <= endDate)
            .OrderBy(vi => vi.Voucher!.VoucherDate)
            .ToListAsync();
    }

    public async Task<List<VoucherItem>> GetBankBookAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.VoucherItems
            .Include(vi => vi.Voucher)
            .Include(vi => vi.Ledger)
            .Where(vi => vi.Voucher!.IsActive && (vi.Ledger!.LedgerName.Contains("Bank") || vi.Ledger.AccountGroup!.GroupName.Contains("Bank")) && vi.Voucher.VoucherDate >= startDate && vi.Voucher.VoucherDate <= endDate)
            .OrderBy(vi => vi.Voucher!.VoucherDate)
            .ToListAsync();
    }

    public async Task<List<VoucherItem>> GetLedgerReportAsync(int ledgerId, DateTime startDate, DateTime endDate)
    {
        return await _context.VoucherItems
            .Include(vi => vi.Voucher)
            .Include(vi => vi.Ledger)
            .Where(vi => vi.LedgerId == ledgerId && vi.Voucher!.IsActive && vi.Voucher.VoucherDate >= startDate && vi.Voucher.VoucherDate <= endDate)
            .OrderBy(vi => vi.Voucher!.VoucherDate)
            .ToListAsync();
    }
}
