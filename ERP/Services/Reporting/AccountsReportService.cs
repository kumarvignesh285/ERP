using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Helpers;
using ERP.Interfaces;
using ERP.Interfaces.Reporting;
using ERP.ViewModels.Reporting;

namespace ERP.Services.Reporting;

public class AccountsReportService : IAccountsReportService
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;

    public AccountsReportService(AppDbContext context, ICompanyContext companyContext)
    {
        _context = context;
        _companyContext = companyContext;
    }

    private int? ActiveCompanyId => _companyContext.CurrentCompanyId;

    public async Task<List<OutstandingReportDto>> GetCustomerOutstandingAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var custQuery = _context.Customers.AsNoTracking().Where(c => c.IsActive);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            custQuery = custQuery.Where(c => c.CompanyId == ActiveCompanyId.Value);

        if (filter.CustomerId.HasValue && filter.CustomerId.Value > 0)
            custQuery = custQuery.Where(c => c.Id == filter.CustomerId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            custQuery = custQuery.Where(c => c.CustomerName.ToLower().Contains(search) || c.CustomerCode.ToLower().Contains(search));
        }

        var customers = await custQuery.ToListAsync(ct);

        var salesQuery = _context.SalesInvoices.AsNoTracking().Where(s => s.IsActive && s.Status != "Cancelled");
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            salesQuery = salesQuery.Where(s => s.CompanyId == ActiveCompanyId.Value);

        var invoicesByCustomer = await salesQuery
            .GroupBy(s => s.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalInvoiced = g.Sum(x => x.GrandTotal),
                TotalPaid = g.Sum(x => x.PaidAmount),
                TotalBalance = g.Sum(x => x.BalanceAmount),
                OverdueCount = g.Count(x => x.DueDate.HasValue && x.DueDate.Value < DateTime.Today && x.BalanceAmount > 0)
            })
            .ToDictionaryAsync(x => x.CustomerId, x => x, ct);

        var result = new List<OutstandingReportDto>();
        foreach (var c in customers)
        {
            decimal invoiced = 0m, paid = 0m, invoiceBalance = 0m;
            int overdueCount = 0;

            if (invoicesByCustomer.TryGetValue(c.Id, out var stats))
            {
                invoiced = stats.TotalInvoiced;
                paid = stats.TotalPaid;
                invoiceBalance = stats.TotalBalance;
                overdueCount = stats.OverdueCount;
            }

            decimal openingDr = c.BalanceType == "Dr" ? c.OpeningBalance : -c.OpeningBalance;
            decimal netOutstanding = openingDr + invoiceBalance;

            if (netOutstanding > 0 || invoiced > 0)
            {
                result.Add(new OutstandingReportDto
                {
                    EntityId = c.Id,
                    EntityCode = c.CustomerCode,
                    EntityName = c.CustomerName,
                    ContactPhone = c.MobileNumber,
                    City = c.City,
                    OpeningBalance = c.OpeningBalance,
                    BalanceType = c.BalanceType,
                    TotalInvoiced = invoiced,
                    TotalPaid = paid,
                    NetOutstanding = netOutstanding,
                    OverdueBillsCount = overdueCount,
                    CreditDays = c.CreditDays,
                    CreditLimit = c.CreditLimit
                });
            }
        }

        return result.OrderByDescending(x => x.NetOutstanding).ToList();
    }

    public async Task<List<OutstandingReportDto>> GetSupplierOutstandingAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var suppQuery = _context.Suppliers.AsNoTracking().Where(s => s.IsActive);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            suppQuery = suppQuery.Where(s => s.CompanyId == ActiveCompanyId.Value);

        if (filter.SupplierId.HasValue && filter.SupplierId.Value > 0)
            suppQuery = suppQuery.Where(s => s.Id == filter.SupplierId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            suppQuery = suppQuery.Where(s => s.SupplierName.ToLower().Contains(search) || s.SupplierCode.ToLower().Contains(search));
        }

        var suppliers = await suppQuery.ToListAsync(ct);

        var purchaseQuery = _context.PurchaseInvoices.AsNoTracking().Where(p => p.IsActive && p.Status != "Cancelled");
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            purchaseQuery = purchaseQuery.Where(p => p.CompanyId == ActiveCompanyId.Value);

        var billsBySupplier = await purchaseQuery
            .GroupBy(p => p.SupplierId)
            .Select(g => new
            {
                SupplierId = g.Key,
                TotalInvoiced = g.Sum(x => x.GrandTotal),
                TotalPaid = g.Sum(x => x.PaidAmount),
                TotalBalance = g.Sum(x => x.BalanceAmount),
                OverdueCount = g.Count(x => x.DueDate.HasValue && x.DueDate.Value < DateTime.Today && x.BalanceAmount > 0)
            })
            .ToDictionaryAsync(x => x.SupplierId, x => x, ct);

        var result = new List<OutstandingReportDto>();
        foreach (var s in suppliers)
        {
            decimal billed = 0m, paid = 0m, billBalance = 0m;
            int overdueCount = 0;

            if (billsBySupplier.TryGetValue(s.Id, out var stats))
            {
                billed = stats.TotalInvoiced;
                paid = stats.TotalPaid;
                billBalance = stats.TotalBalance;
                overdueCount = stats.OverdueCount;
            }

            decimal openingCr = s.BalanceType == "Cr" ? s.OpeningBalance : -s.OpeningBalance;
            decimal netOutstanding = openingCr + billBalance;

            if (netOutstanding > 0 || billed > 0)
            {
                result.Add(new OutstandingReportDto
                {
                    EntityId = s.Id,
                    EntityCode = s.SupplierCode,
                    EntityName = s.SupplierName,
                    ContactPhone = s.Mobile,
                    City = s.City,
                    OpeningBalance = s.OpeningBalance,
                    BalanceType = s.BalanceType,
                    TotalInvoiced = billed,
                    TotalPaid = paid,
                    NetOutstanding = netOutstanding,
                    OverdueBillsCount = overdueCount,
                    CreditDays = s.CreditDays,
                    CreditLimit = s.CreditLimit
                });
            }
        }

        return result.OrderByDescending(x => x.NetOutstanding).ToList();
    }

    public async Task<List<LedgerStatementRowDto>> GetCustomerLedgerStatementAsync(int customerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId, ct);
        if (customer == null) return new List<LedgerStatementRowDto>();

        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        // Pre-period sales invoices balance
        var priorSales = await _context.SalesInvoices.AsNoTracking()
            .Where(s => s.CustomerId == customerId && s.IsActive && s.Status != "Cancelled" && s.InvoiceDate < from)
            .SumAsync(s => (decimal?)(s.GrandTotal - s.PaidAmount), ct) ?? 0m;

        decimal initialBalance = (customer.BalanceType == "Dr" ? customer.OpeningBalance : -customer.OpeningBalance) + priorSales;

        var invoices = await _context.SalesInvoices.AsNoTracking()
            .Where(s => s.CustomerId == customerId && s.IsActive && s.Status != "Cancelled" && s.InvoiceDate >= from && s.InvoiceDate <= to)
            .OrderBy(s => s.InvoiceDate)
            .ToListAsync(ct);

        var returns = await _context.SalesReturns.AsNoTracking()
            .Where(r => r.CustomerId == customerId && r.IsActive && r.Status != "Cancelled" && r.ReturnDate >= from && r.ReturnDate <= to)
            .OrderBy(r => r.ReturnDate)
            .ToListAsync(ct);

        var result = new List<LedgerStatementRowDto>
        {
            new()
            {
                Date = from,
                ReferenceNumber = "OPENING",
                VoucherType = "Opening Balance",
                Particulars = "Opening Balance as of start date",
                Debit = initialBalance >= 0 ? initialBalance : 0,
                Credit = initialBalance < 0 ? Math.Abs(initialBalance) : 0,
                RunningBalance = Math.Abs(initialBalance),
                BalanceType = initialBalance >= 0 ? "Dr" : "Cr"
            }
        };

        // Combine chronologically
        var entries = new List<(DateTime Date, string Ref, string Type, string Particulars, decimal Debit, decimal Credit)>();

        foreach (var inv in invoices)
        {
            entries.Add((inv.InvoiceDate, inv.InvoiceNumber, "Sales Invoice", $"Invoice #{inv.InvoiceNumber} [Status: {inv.Status}]", inv.GrandTotal, 0));
            if (inv.PaidAmount > 0)
            {
                entries.Add((inv.InvoiceDate, inv.InvoiceNumber, "Payment Received", $"Collection for Inv #{inv.InvoiceNumber}", 0, inv.PaidAmount));
            }
        }

        foreach (var ret in returns)
        {
            entries.Add((ret.ReturnDate, ret.ReturnNumber, "Sales Return", $"Return #{ret.ReturnNumber} Reason: {ret.Reason}", 0, ret.GrandTotal));
        }

        decimal running = initialBalance;
        foreach (var e in entries.OrderBy(x => x.Date))
        {
            running += (e.Debit - e.Credit);
            result.Add(new LedgerStatementRowDto
            {
                Date = e.Date,
                ReferenceNumber = e.Ref,
                VoucherType = e.Type,
                Particulars = e.Particulars,
                Debit = e.Debit,
                Credit = e.Credit,
                RunningBalance = Math.Abs(running),
                BalanceType = running >= 0 ? "Dr" : "Cr"
            });
        }

        return result;
    }

    public async Task<List<LedgerStatementRowDto>> GetSupplierLedgerStatementAsync(int supplierId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var supplier = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == supplierId, ct);
        if (supplier == null) return new List<LedgerStatementRowDto>();

        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var priorPurchases = await _context.PurchaseInvoices.AsNoTracking()
            .Where(p => p.SupplierId == supplierId && p.IsActive && p.Status != "Cancelled" && p.InvoiceDate < from)
            .SumAsync(p => (decimal?)(p.GrandTotal - p.PaidAmount), ct) ?? 0m;

        decimal initialBalance = (supplier.BalanceType == "Cr" ? supplier.OpeningBalance : -supplier.OpeningBalance) + priorPurchases;

        var bills = await _context.PurchaseInvoices.AsNoTracking()
            .Where(p => p.SupplierId == supplierId && p.IsActive && p.Status != "Cancelled" && p.InvoiceDate >= from && p.InvoiceDate <= to)
            .OrderBy(p => p.InvoiceDate)
            .ToListAsync(ct);

        var returns = await _context.PurchaseReturns.AsNoTracking()
            .Where(r => r.SupplierId == supplierId && r.IsActive && r.Status != "Cancelled" && r.ReturnDate >= from && r.ReturnDate <= to)
            .OrderBy(r => r.ReturnDate)
            .ToListAsync(ct);

        var result = new List<LedgerStatementRowDto>
        {
            new()
            {
                Date = from,
                ReferenceNumber = "OPENING",
                VoucherType = "Opening Balance",
                Particulars = "Opening Balance as of start date",
                Debit = initialBalance < 0 ? Math.Abs(initialBalance) : 0,
                Credit = initialBalance >= 0 ? initialBalance : 0,
                RunningBalance = Math.Abs(initialBalance),
                BalanceType = initialBalance >= 0 ? "Cr" : "Dr"
            }
        };

        var entries = new List<(DateTime Date, string Ref, string Type, string Particulars, decimal Debit, decimal Credit)>();

        foreach (var b in bills)
        {
            entries.Add((b.InvoiceDate, b.InvoiceNumber, "Purchase Bill", $"Purchase Bill #{b.InvoiceNumber}", 0, b.GrandTotal));
            if (b.PaidAmount > 0)
            {
                entries.Add((b.InvoiceDate, b.InvoiceNumber, "Payment Made", $"Payment for Bill #{b.InvoiceNumber}", b.PaidAmount, 0));
            }
        }

        foreach (var ret in returns)
        {
            entries.Add((ret.ReturnDate, ret.ReturnNumber, "Purchase Return", $"Return #{ret.ReturnNumber} Reason: {ret.Reason}", ret.GrandTotal, 0));
        }

        decimal running = initialBalance;
        foreach (var e in entries.OrderBy(x => x.Date))
        {
            running += (e.Credit - e.Debit);
            result.Add(new LedgerStatementRowDto
            {
                Date = e.Date,
                ReferenceNumber = e.Ref,
                VoucherType = e.Type,
                Particulars = e.Particulars,
                Debit = e.Debit,
                Credit = e.Credit,
                RunningBalance = Math.Abs(running),
                BalanceType = running >= 0 ? "Cr" : "Dr"
            });
        }

        return result;
    }

    public async Task<List<CashBankBookRowDto>> GetCashBookAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.VoucherItems.AsNoTracking()
            .Include(vi => vi.Voucher)
            .Include(vi => vi.Ledger)
            .Where(vi => vi.Voucher != null && vi.Voucher.IsActive && vi.Ledger != null &&
                        (vi.Ledger.LedgerName.Contains("Cash") || (vi.Ledger.AccountGroup != null && vi.Ledger.AccountGroup.GroupName.Contains("Cash"))) &&
                        vi.Voucher.VoucherDate >= from && vi.Voucher.VoucherDate <= to);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(vi => vi.Voucher!.CompanyId == ActiveCompanyId.Value);

        var items = await query.OrderBy(vi => vi.Voucher!.VoucherDate).ThenBy(vi => vi.Id).ToListAsync(ct);

        var result = new List<CashBankBookRowDto>();
        decimal running = 0m;

        foreach (var item in items)
        {
            running += (item.DebitAmount - item.CreditAmount);
            result.Add(new CashBankBookRowDto
            {
                Date = item.Voucher!.VoucherDate,
                VoucherNumber = item.Voucher.VoucherNumber,
                VoucherType = item.Voucher.VoucherType,
                LedgerName = item.Ledger?.LedgerName ?? "Cash",
                Narration = item.Particulars ?? item.Voucher.Narration,
                Receipts = item.DebitAmount,
                Payments = item.CreditAmount,
                ClosingBalance = running
            });
        }

        return result;
    }

    public async Task<List<CashBankBookRowDto>> GetBankBookAsync(DateTime fromDate, DateTime toDate, int? bankLedgerId = null, CancellationToken ct = default)
    {
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.VoucherItems.AsNoTracking()
            .Include(vi => vi.Voucher)
            .Include(vi => vi.Ledger)
            .Where(vi => vi.Voucher != null && vi.Voucher.IsActive && vi.Ledger != null &&
                        (vi.Ledger.LedgerName.Contains("Bank") || (vi.Ledger.AccountGroup != null && vi.Ledger.AccountGroup.GroupName.Contains("Bank"))) &&
                        vi.Voucher.VoucherDate >= from && vi.Voucher.VoucherDate <= to);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(vi => vi.Voucher!.CompanyId == ActiveCompanyId.Value);

        if (bankLedgerId.HasValue && bankLedgerId.Value > 0)
            query = query.Where(vi => vi.LedgerId == bankLedgerId.Value);

        var items = await query.OrderBy(vi => vi.Voucher!.VoucherDate).ThenBy(vi => vi.Id).ToListAsync(ct);

        var result = new List<CashBankBookRowDto>();
        decimal running = 0m;

        foreach (var item in items)
        {
            running += (item.DebitAmount - item.CreditAmount);
            result.Add(new CashBankBookRowDto
            {
                Date = item.Voucher!.VoucherDate,
                VoucherNumber = item.Voucher.VoucherNumber,
                VoucherType = item.Voucher.VoucherType,
                LedgerName = item.Ledger?.LedgerName ?? "Bank",
                Narration = item.Particulars ?? item.Voucher.Narration,
                Receipts = item.DebitAmount,
                Payments = item.CreditAmount,
                ClosingBalance = running
            });
        }

        return result;
    }

    public async Task<List<VoucherReportDto>> GetReceiptVouchersAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        return await GetVouchersByTypeAsync("Receipt", filter, ct);
    }

    public async Task<List<VoucherReportDto>> GetPaymentVouchersAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        return await GetVouchersByTypeAsync("Payment", filter, ct);
    }

    public async Task<List<VoucherReportDto>> GetJournalVouchersAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        return await GetVouchersByTypeAsync("Journal", filter, ct);
    }

    private async Task<List<VoucherReportDto>> GetVouchersByTypeAsync(string type, ReportFilterViewModel filter, CancellationToken ct)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.Vouchers.AsNoTracking()
            .Include(v => v.Items)
                .ThenInclude(i => i.Ledger)
            .Where(v => v.IsActive && v.VoucherType == type && v.VoucherDate >= fromDate && v.VoucherDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(v => v.CompanyId == ActiveCompanyId.Value);

        return await query
            .OrderByDescending(v => v.VoucherDate)
            .Select(v => new VoucherReportDto
            {
                VoucherId = v.Id,
                VoucherNumber = v.VoucherNumber,
                VoucherDate = v.VoucherDate,
                VoucherType = v.VoucherType,
                ReferenceNumber = v.ReferenceNumber,
                Narration = v.Narration,
                TotalAmount = v.TotalAmount,
                AccountSummary = string.Join(", ", v.Items.Select(i => (i.Ledger != null ? i.Ledger.LedgerName : "Ledger") + (i.DebitAmount > 0 ? " (Dr)" : " (Cr)")))
            })
            .ToListAsync(ct);
    }

    public async Task<List<LedgerSummaryDto>> GetLedgerSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var ledgersQuery = _context.Ledgers.AsNoTracking().Include(l => l.AccountGroup).Where(l => l.IsActive);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            ledgersQuery = ledgersQuery.Where(l => l.CompanyId == ActiveCompanyId.Value);

        var ledgers = await ledgersQuery.ToListAsync(ct);

        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var viQuery = _context.VoucherItems.AsNoTracking()
            .Include(vi => vi.Voucher)
            .Where(vi => vi.Voucher != null && vi.Voucher.IsActive && vi.Voucher.VoucherDate >= fromDate && vi.Voucher.VoucherDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            viQuery = viQuery.Where(vi => vi.Voucher!.CompanyId == ActiveCompanyId.Value);

        var groupedTxs = await viQuery
            .GroupBy(vi => vi.LedgerId)
            .Select(g => new
            {
                LedgerId = g.Key,
                TotalDebit = g.Sum(x => x.DebitAmount),
                TotalCredit = g.Sum(x => x.CreditAmount)
            })
            .ToDictionaryAsync(x => x.LedgerId, x => x, ct);

        var result = new List<LedgerSummaryDto>();
        foreach (var l in ledgers)
        {
            decimal dr = 0m, cr = 0m;
            if (groupedTxs.TryGetValue(l.Id, out var stats))
            {
                dr = stats.TotalDebit;
                cr = stats.TotalCredit;
            }

            decimal net = l.BalanceType == "Dr" ? (l.OpeningBalance + dr - cr) : (l.OpeningBalance + cr - dr);
            string closingType = net >= 0 ? l.BalanceType : (l.BalanceType == "Dr" ? "Cr" : "Dr");

            result.Add(new LedgerSummaryDto
            {
                LedgerId = l.Id,
                LedgerCode = l.LedgerCode,
                LedgerName = l.LedgerName,
                GroupName = l.AccountGroup?.GroupName ?? "General",
                OpeningBalance = l.OpeningBalance,
                OpeningType = l.BalanceType,
                TotalDebit = dr,
                TotalCredit = cr,
                ClosingBalance = Math.Abs(net),
                ClosingType = closingType
            });
        }

        return result.OrderBy(x => x.GroupName).ThenBy(x => x.LedgerName).ToList();
    }

    public async Task<List<TrialBalanceRowDto>> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken ct = default)
    {
        var filter = new ReportFilterViewModel { FromDate = DateTime.MinValue, ToDate = asOfDate };
        var summaries = await GetLedgerSummaryAsync(filter, ct);

        return summaries.Select(s => new TrialBalanceRowDto
        {
            LedgerId = s.LedgerId,
            LedgerName = s.LedgerName,
            AccountGroup = s.GroupName,
            DebitAmount = s.ClosingType == "Dr" ? s.ClosingBalance : 0m,
            CreditAmount = s.ClosingType == "Cr" ? s.ClosingBalance : 0m
        }).ToList();
    }

    public async Task<ProfitAndLossReportDto> GetProfitAndLossAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var salesQuery = _context.SalesInvoices.AsNoTracking()
            .Where(s => s.IsActive && s.Status != "Cancelled" && s.InvoiceDate >= from && s.InvoiceDate <= to);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            salesQuery = salesQuery.Where(s => s.CompanyId == ActiveCompanyId.Value);

        var totalSales = await salesQuery.SumAsync(s => (decimal?)s.GrandTotal, ct) ?? 0m;

        var salesReturnsQuery = _context.SalesReturns.AsNoTracking()
            .Where(r => r.IsActive && r.Status != "Cancelled" && r.ReturnDate >= from && r.ReturnDate <= to);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            salesReturnsQuery = salesReturnsQuery.Where(r => r.CompanyId == ActiveCompanyId.Value);

        var totalSalesReturns = await salesReturnsQuery.SumAsync(r => (decimal?)r.GrandTotal, ct) ?? 0m;

        // Purchases as COGS proxy
        var purchaseQuery = _context.PurchaseInvoices.AsNoTracking()
            .Where(p => p.IsActive && p.Status != "Cancelled" && p.InvoiceDate >= from && p.InvoiceDate <= to);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            purchaseQuery = purchaseQuery.Where(p => p.CompanyId == ActiveCompanyId.Value);

        var totalPurchases = await purchaseQuery.SumAsync(p => (decimal?)p.GrandTotal, ct) ?? 0m;

        var purchaseReturnsQuery = _context.PurchaseReturns.AsNoTracking()
            .Where(r => r.IsActive && r.Status != "Cancelled" && r.ReturnDate >= from && r.ReturnDate <= to);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            purchaseReturnsQuery = purchaseReturnsQuery.Where(r => r.CompanyId == ActiveCompanyId.Value);

        var totalPurchaseReturns = await purchaseReturnsQuery.SumAsync(r => (decimal?)r.GrandTotal, ct) ?? 0m;

        var netPurchases = totalPurchases - totalPurchaseReturns;

        return new ProfitAndLossReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            SalesRevenue = totalSales,
            SalesReturns = totalSalesReturns,
            CostOfGoodsSold = netPurchases,
            IndirectIncomes = 0m,
            OperatingExpenses = 0m
        };
    }

    public async Task<List<ReportSummaryCard>> GetAccountsSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var custOutstanding = await GetCustomerOutstandingAsync(filter, ct);
        var suppOutstanding = await GetSupplierOutstandingAsync(filter, ct);

        var totalReceivables = custOutstanding.Sum(x => x.NetOutstanding);
        var totalPayables = suppOutstanding.Sum(x => x.NetOutstanding);

        return new List<ReportSummaryCard>
        {
            new() { Title = "Total Receivables", Value = $"₹{totalReceivables:N2}", Icon = "fa-hand-holding-dollar", ColorClass = "primary" },
            new() { Title = "Total Payables", Value = $"₹{totalPayables:N2}", Icon = "fa-money-bill-wave", ColorClass = "warning" },
            new() { Title = "Debtors with Balance", Value = custOutstanding.Count.ToString("N0"), Icon = "fa-users", ColorClass = "info" },
            new() { Title = "Creditors with Balance", Value = suppOutstanding.Count.ToString("N0"), Icon = "fa-truck", ColorClass = "success" }
        };
    }
}
