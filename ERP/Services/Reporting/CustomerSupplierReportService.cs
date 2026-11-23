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

public class CustomerSupplierReportService : ICustomerSupplierReportService
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;
    private readonly ISalesReportService _salesReportService;
    private readonly IPurchaseReportService _purchaseReportService;
    private readonly IAccountsReportService _accountsReportService;

    public CustomerSupplierReportService(
        AppDbContext context,
        ICompanyContext companyContext,
        ISalesReportService salesReportService,
        IPurchaseReportService purchaseReportService,
        IAccountsReportService accountsReportService)
    {
        _context = context;
        _companyContext = companyContext;
        _salesReportService = salesReportService;
        _purchaseReportService = purchaseReportService;
        _accountsReportService = accountsReportService;
    }

    private int? ActiveCompanyId => _companyContext.CurrentCompanyId;

    public async Task<List<CustomerMasterReportDto>> GetCustomerListAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = _context.Customers.AsNoTracking().Where(c => c.IsActive);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(c => c.CompanyId == ActiveCompanyId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(c => c.CustomerName.ToLower().Contains(search) || c.CustomerCode.ToLower().Contains(search) || (c.MobileNumber != null && c.MobileNumber.Contains(search)));
        }

        var customers = await query.OrderBy(c => c.CustomerName).ToListAsync(ct);

        var outstanding = await _accountsReportService.GetCustomerOutstandingAsync(filter, ct);
        var balanceDict = outstanding.ToDictionary(x => x.EntityId, x => x.NetOutstanding);

        return customers.Select(c => new CustomerMasterReportDto
        {
            CustomerId = c.Id,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            MobileNumber = c.MobileNumber,
            Email = c.Email,
            GSTNumber = c.GSTNumber,
            City = c.City,
            State = c.State,
            CreditLimit = c.CreditLimit,
            CurrentBalance = balanceDict.TryGetValue(c.Id, out var bal) ? bal : c.OpeningBalance,
            CreatedAt = c.CreatedAt,
            IsActive = c.IsActive
        }).ToList();
    }

    public async Task<List<CustomerMasterReportDto>> GetNewCustomersAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.Customers.AsNoTracking()
            .Where(c => c.IsActive && c.CreatedAt >= fromDate && c.CreatedAt <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(c => c.CompanyId == ActiveCompanyId.Value);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CustomerMasterReportDto
            {
                CustomerId = c.Id,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                MobileNumber = c.MobileNumber,
                Email = c.Email,
                GSTNumber = c.GSTNumber,
                City = c.City,
                State = c.State,
                CreditLimit = c.CreditLimit,
                CurrentBalance = c.OpeningBalance,
                CreatedAt = c.CreatedAt,
                IsActive = c.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<List<CustomerWiseSalesDto>> GetTopCustomersAsync(ReportFilterViewModel filter, int limit = 20, CancellationToken ct = default)
    {
        var list = await _salesReportService.GetCustomerWiseSalesAsync(filter, ct);
        return list.Take(limit).ToList();
    }

    public async Task<List<SalesRegisterDto>> GetCustomerPurchaseHistoryAsync(int customerId, ReportFilterViewModel filter, CancellationToken ct = default)
    {
        filter.CustomerId = customerId;
        return await _salesReportService.GetSalesRegisterAsync(filter, ct);
    }

    public async Task<List<OutstandingReportDto>> GetCustomerOutstandingAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        return await _accountsReportService.GetCustomerOutstandingAsync(filter, ct);
    }

    public async Task<List<LedgerStatementRowDto>> GetCustomerLedgerAsync(int customerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        return await _accountsReportService.GetCustomerLedgerStatementAsync(customerId, fromDate, toDate, ct);
    }

    public async Task<List<InactiveEntityDto>> GetInactiveCustomersAsync(ReportFilterViewModel filter, int inactiveDays = 60, CancellationToken ct = default)
    {
        var cutoff = DateTime.Today.AddDays(-inactiveDays);

        var custQuery = _context.Customers.AsNoTracking().Where(c => c.IsActive);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            custQuery = custQuery.Where(c => c.CompanyId == ActiveCompanyId.Value);

        var customers = await custQuery.ToListAsync(ct);

        var salesQuery = _context.SalesInvoices.AsNoTracking().Where(s => s.IsActive && s.Status != "Cancelled");
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            salesQuery = salesQuery.Where(s => s.CompanyId == ActiveCompanyId.Value);

        var customerTxs = await salesQuery
            .GroupBy(s => s.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                LastDate = g.Max(x => x.InvoiceDate),
                LifetimeSales = g.Sum(x => x.GrandTotal),
                Outstanding = g.Sum(x => x.BalanceAmount)
            })
            .ToDictionaryAsync(x => x.CustomerId, x => x, ct);

        var result = new List<InactiveEntityDto>();
        foreach (var c in customers)
        {
            if (customerTxs.TryGetValue(c.Id, out var tx))
            {
                if (tx.LastDate < cutoff)
                {
                    result.Add(new InactiveEntityDto
                    {
                        EntityId = c.Id,
                        EntityCode = c.CustomerCode,
                        EntityName = c.CustomerName,
                        Mobile = c.MobileNumber,
                        Email = c.Email,
                        LastTransactionDate = tx.LastDate,
                        LifetimeSalesAmount = tx.LifetimeSales,
                        OutstandingAmount = tx.Outstanding
                    });
                }
            }
            else
            {
                // Never made a purchase
                result.Add(new InactiveEntityDto
                {
                    EntityId = c.Id,
                    EntityCode = c.CustomerCode,
                    EntityName = c.CustomerName,
                    Mobile = c.MobileNumber,
                    Email = c.Email,
                    LastTransactionDate = null,
                    LifetimeSalesAmount = 0m,
                    OutstandingAmount = c.OpeningBalance
                });
            }
        }

        return result.OrderByDescending(x => x.DaysSinceLastTransaction).ToList();
    }

    public async Task<List<SupplierMasterReportDto>> GetSupplierListAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = _context.Suppliers.AsNoTracking().Where(s => s.IsActive);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(s => s.CompanyId == ActiveCompanyId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(s => s.SupplierName.ToLower().Contains(search) || s.SupplierCode.ToLower().Contains(search) || (s.Mobile != null && s.Mobile.Contains(search)));
        }

        var suppliers = await query.OrderBy(s => s.SupplierName).ToListAsync(ct);

        var outstanding = await _accountsReportService.GetSupplierOutstandingAsync(filter, ct);
        var balanceDict = outstanding.ToDictionary(x => x.EntityId, x => x.NetOutstanding);

        return suppliers.Select(s => new SupplierMasterReportDto
        {
            SupplierId = s.Id,
            SupplierCode = s.SupplierCode,
            SupplierName = s.SupplierName,
            Mobile = s.Mobile,
            Email = s.Email,
            GSTNumber = s.GSTNumber,
            City = s.City,
            State = s.State,
            CreditLimit = s.CreditLimit,
            CurrentBalance = balanceDict.TryGetValue(s.Id, out var bal) ? bal : s.OpeningBalance,
            CreatedAt = s.CreatedAt,
            IsActive = s.IsActive
        }).ToList();
    }

    public async Task<List<SupplierWisePurchaseDto>> GetTopSuppliersAsync(ReportFilterViewModel filter, int limit = 20, CancellationToken ct = default)
    {
        var list = await _purchaseReportService.GetSupplierWisePurchaseAsync(filter, ct);
        return list.Take(limit).ToList();
    }

    public async Task<List<PurchaseRegisterDto>> GetSupplierPurchaseHistoryAsync(int supplierId, ReportFilterViewModel filter, CancellationToken ct = default)
    {
        filter.SupplierId = supplierId;
        return await _purchaseReportService.GetPurchaseRegisterAsync(filter, ct);
    }

    public async Task<List<OutstandingReportDto>> GetSupplierOutstandingAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        return await _accountsReportService.GetSupplierOutstandingAsync(filter, ct);
    }

    public async Task<List<LedgerStatementRowDto>> GetSupplierLedgerAsync(int supplierId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        return await _accountsReportService.GetSupplierLedgerStatementAsync(supplierId, fromDate, toDate, ct);
    }

    public async Task<List<PurchasePaymentReportDto>> GetSupplierPaymentHistoryAsync(int supplierId, ReportFilterViewModel filter, CancellationToken ct = default)
    {
        filter.SupplierId = supplierId;
        return await _purchaseReportService.GetPurchasePaymentReportAsync(filter, ct);
    }

    public async Task<List<ReportSummaryCard>> GetCustomerSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = _context.Customers.AsNoTracking().Where(c => c.IsActive);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(c => c.CompanyId == ActiveCompanyId.Value);

        var totalCust = await query.CountAsync(ct);
        var outstanding = await _accountsReportService.GetCustomerOutstandingAsync(filter, ct);
        var totalOutstanding = outstanding.Sum(x => x.NetOutstanding);
        var overdueCount = outstanding.Count(x => x.OverdueBillsCount > 0);

        return new List<ReportSummaryCard>
        {
            new() { Title = "Total Customers", Value = totalCust.ToString("N0"), Icon = "fa-users", ColorClass = "primary" },
            new() { Title = "Active Debtors", Value = outstanding.Count.ToString("N0"), Icon = "fa-user-check", ColorClass = "info" },
            new() { Title = "Total Receivables", Value = $"₹{totalOutstanding:N2}", Icon = "fa-hand-holding-dollar", ColorClass = "success" },
            new() { Title = "Customers Overdue", Value = overdueCount.ToString("N0"), Icon = "fa-user-clock", ColorClass = "danger" }
        };
    }

    public async Task<List<ReportSummaryCard>> GetSupplierSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = _context.Suppliers.AsNoTracking().Where(s => s.IsActive);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(s => s.CompanyId == ActiveCompanyId.Value);

        var totalSupp = await query.CountAsync(ct);
        var outstanding = await _accountsReportService.GetSupplierOutstandingAsync(filter, ct);
        var totalOutstanding = outstanding.Sum(x => x.NetOutstanding);

        return new List<ReportSummaryCard>
        {
            new() { Title = "Total Suppliers", Value = totalSupp.ToString("N0"), Icon = "fa-truck-field", ColorClass = "primary" },
            new() { Title = "Active Creditors", Value = outstanding.Count.ToString("N0"), Icon = "fa-truck", ColorClass = "info" },
            new() { Title = "Total Payables", Value = $"₹{totalOutstanding:N2}", Icon = "fa-money-bill-transfer", ColorClass = "warning" }
        };
    }
}
