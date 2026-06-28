using ERP.Models;

namespace ERP.Interfaces;

public interface IAccountingService
{
    // Vouchers
    Task<List<Voucher>> GetVouchersAsync(string? type = null);
    Task<Voucher?> GetVoucherByIdAsync(int id);
    Task<Voucher> SaveVoucherAsync(Voucher voucher);
    Task DeleteVoucherAsync(int id);

    // Financial Statements
    Task<List<LedgerBalance>> GetTrialBalanceAsync();
    Task<ProfitAndLossStatement> GetProfitAndLossAsync(DateTime startDate, DateTime endDate);
    Task<BalanceSheetStatement> GetBalanceSheetAsync(DateTime date);

    // Cash and Bank Book
    Task<List<VoucherItem>> GetCashBookAsync(DateTime startDate, DateTime endDate);
    Task<List<VoucherItem>> GetBankBookAsync(DateTime startDate, DateTime endDate);
    Task<List<VoucherItem>> GetLedgerReportAsync(int ledgerId, DateTime startDate, DateTime endDate);
}

public class LedgerBalance
{
    public int LedgerId { get; set; }
    public string LedgerCode { get; set; } = string.Empty;
    public string LedgerName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string BalanceType { get; set; } = "Dr";
}

public class ProfitAndLossStatement
{
    public decimal GrossSales { get; set; }
    public decimal SalesReturns { get; set; }
    public decimal NetSales => GrossSales - SalesReturns;

    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit => NetSales - CostOfGoodsSold;

    public List<LedgerBalance> IndirectExpenses { get; set; } = new();
    public decimal TotalIndirectExpenses { get; set; }

    public List<LedgerBalance> IndirectIncomes { get; set; } = new();
    public decimal TotalIndirectIncomes { get; set; }

    public decimal NetProfit => GrossProfit + TotalIndirectIncomes - TotalIndirectExpenses;
}

public class BalanceSheetStatement
{
    public List<LedgerBalance> Assets { get; set; } = new();
    public decimal TotalAssets { get; set; }

    public List<LedgerBalance> Liabilities { get; set; } = new();
    public decimal TotalLiabilities { get; set; }

    public List<LedgerBalance> Equities { get; set; } = new();
    public decimal TotalEquities { get; set; }
}
