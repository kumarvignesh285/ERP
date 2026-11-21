using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Services;

public class ERPReportService : IERPReportService
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;

    public ERPReportService(AppDbContext context, ICompanyContext companyContext)
    {
        _context = context;
        _companyContext = companyContext;
    }

    public int GetActiveCompanyId()
    {
        return _companyContext.CurrentCompanyId ?? 0;
    }

    public string GetActiveCompanyName()
    {
        return _companyContext.CurrentCompanyName ?? "All Companies (System)";
    }

    private int RequireCompanyId()
    {
        var compId = GetActiveCompanyId();
        return compId;
    }

    #region Sales Reports

    public async Task<SalesSummaryResultDto> GetSalesSummaryAsync(
        DateTime fromDate, 
        DateTime toDate, 
        int? customerId = null, 
        int? categoryId = null, 
        int? brandId = null, 
        int? productId = null, 
        CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.SalesInvoices.AsNoTracking()
            .Where(s => s.IsActive && s.Status != "Cancelled" && s.InvoiceDate >= from && s.InvoiceDate <= to);

        if (compId > 0)
            query = query.Where(s => s.CompanyId == compId);

        if (customerId.HasValue && customerId.Value > 0)
            query = query.Where(s => s.CustomerId == customerId.Value);

        if (productId.HasValue || categoryId.HasValue || brandId.HasValue)
        {
            query = query.Where(s => s.Items.Any(i => 
                (!productId.HasValue || i.ProductId == productId.Value) &&
                (!categoryId.HasValue || (i.Product != null && i.Product.CategoryId == categoryId.Value)) &&
                (!brandId.HasValue || (i.Product != null && i.Product.BrandId == brandId.Value))));
        }

        var invoices = await query.ToListAsync(ct);

        var returnQuery = _context.SalesReturns.AsNoTracking()
            .Where(r => r.IsActive && r.Status != "Cancelled" && r.ReturnDate >= from && r.ReturnDate <= to);
        if (compId > 0)
            returnQuery = returnQuery.Where(r => r.CompanyId == compId);
        if (customerId.HasValue && customerId.Value > 0)
            returnQuery = returnQuery.Where(r => r.CustomerId == customerId.Value);

        var returns = await returnQuery.ToListAsync(ct);

        var itemsQuery = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" && 
                        i.SalesInvoice.InvoiceDate >= from && i.SalesInvoice.InvoiceDate <= to);
        if (compId > 0)
            itemsQuery = itemsQuery.Where(i => i.SalesInvoice!.CompanyId == compId);
        if (customerId.HasValue && customerId.Value > 0)
            itemsQuery = itemsQuery.Where(i => i.SalesInvoice!.CustomerId == customerId.Value);
        if (productId.HasValue && productId.Value > 0)
            itemsQuery = itemsQuery.Where(i => i.ProductId == productId.Value);

        var totalQty = await itemsQuery.SumAsync(i => (decimal?)i.Quantity, ct) ?? 0m;

        var result = new SalesSummaryResultDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalInvoices = invoices.Count,
            GrossSales = invoices.Sum(s => s.SubTotal),
            TotalDiscounts = invoices.Sum(s => s.DiscountAmount),
            TotalTaxes = invoices.Sum(s => s.TaxAmount),
            RoundOff = invoices.Sum(s => s.RoundOff),
            TotalSales = invoices.Sum(s => s.GrandTotal),
            TotalReturns = returns.Sum(r => r.GrandTotal),
            PaidAmount = invoices.Sum(s => s.PaidAmount),
            BalanceAmount = invoices.Sum(s => s.BalanceAmount),
            TotalQuantitySold = totalQty
        };

        return result;
    }

    public async Task<List<SalesByEntityDto>> GetSalesByCustomerAsync(DateTime fromDate, DateTime toDate, int limit = 10, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.SalesInvoices.AsNoTracking()
            .Where(s => s.IsActive && s.Status != "Cancelled" && s.InvoiceDate >= from && s.InvoiceDate <= to);
        if (compId > 0)
            query = query.Where(s => s.CompanyId == compId);

        var totalSales = await query.SumAsync(s => (decimal?)s.GrandTotal, ct) ?? 0m;

        var grouped = await query
            .GroupBy(s => new { s.CustomerId, Name = s.Customer != null ? s.Customer.CustomerName : "Unknown", Code = s.Customer != null ? s.Customer.CustomerCode : "" })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.Name,
                g.Key.Code,
                TotalAmount = g.Sum(s => s.GrandTotal),
                Count = g.Count()
            })
            .OrderByDescending(g => g.TotalAmount)
            .Take(limit)
            .ToListAsync(ct);

        return grouped.Select(g => new SalesByEntityDto
        {
            EntityId = g.CustomerId,
            EntityCode = g.Code,
            EntityName = g.Name,
            Amount = g.TotalAmount,
            InvoiceCount = g.Count,
            Percentage = totalSales > 0 ? Math.Round((g.TotalAmount / totalSales) * 100m, 2) : 0m
        }).ToList();
    }

    public async Task<List<SalesByEntityDto>> GetSalesByProductAsync(DateTime fromDate, DateTime toDate, int limit = 10, bool ascending = false, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= from && i.SalesInvoice.InvoiceDate <= to);
        if (compId > 0)
            query = query.Where(i => i.SalesInvoice!.CompanyId == compId);

        var totalSales = await query.SumAsync(i => (decimal?)i.Amount, ct) ?? 0m;

        var groupedQuery = query
            .GroupBy(i => new { i.ProductId, Name = i.Product != null ? i.Product.ProductName : i.ProductName, Code = i.Product != null ? i.Product.ProductCode : "" })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                g.Key.Code,
                Quantity = g.Sum(i => i.Quantity),
                Amount = g.Sum(i => i.Amount),
                Count = g.Select(i => i.SalesInvoiceId).Distinct().Count()
            });

        var ordered = ascending 
            ? groupedQuery.OrderBy(g => g.Quantity).ThenBy(g => g.Amount)
            : groupedQuery.OrderByDescending(g => g.Quantity).ThenByDescending(g => g.Amount);

        var results = await ordered.Take(limit).ToListAsync(ct);

        return results.Select(g => new SalesByEntityDto
        {
            EntityId = g.ProductId,
            EntityCode = g.Code,
            EntityName = g.Name,
            Quantity = g.Quantity,
            Amount = g.Amount,
            InvoiceCount = g.Count,
            Percentage = totalSales > 0 ? Math.Round((g.Amount / totalSales) * 100m, 2) : 0m
        }).ToList();
    }

    public async Task<List<SalesByEntityDto>> GetSalesByCategoryAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= from && i.SalesInvoice.InvoiceDate <= to);
        if (compId > 0)
            query = query.Where(i => i.SalesInvoice!.CompanyId == compId);

        var totalSales = await query.SumAsync(i => (decimal?)i.Amount, ct) ?? 0m;

        var grouped = await query
            .GroupBy(i => new 
            { 
                CategoryId = i.Product != null && i.Product.CategoryId.HasValue ? i.Product.CategoryId.Value : 0,
                CategoryName = i.Product != null && i.Product.Category != null ? i.Product.Category.CategoryName : "Uncategorized"
            })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.CategoryName,
                Quantity = g.Sum(i => i.Quantity),
                Amount = g.Sum(i => i.Amount),
                Count = g.Select(i => i.SalesInvoiceId).Distinct().Count()
            })
            .OrderByDescending(g => g.Amount)
            .ToListAsync(ct);

        return grouped.Select(g => new SalesByEntityDto
        {
            EntityId = g.CategoryId,
            EntityName = g.CategoryName,
            Quantity = g.Quantity,
            Amount = g.Amount,
            InvoiceCount = g.Count,
            Percentage = totalSales > 0 ? Math.Round((g.Amount / totalSales) * 100m, 2) : 0m
        }).ToList();
    }

    public async Task<List<SalesByEntityDto>> GetSalesByBrandAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= from && i.SalesInvoice.InvoiceDate <= to);
        if (compId > 0)
            query = query.Where(i => i.SalesInvoice!.CompanyId == compId);

        var totalSales = await query.SumAsync(i => (decimal?)i.Amount, ct) ?? 0m;

        var grouped = await query
            .GroupBy(i => new 
            { 
                BrandId = i.Product != null && i.Product.BrandId.HasValue ? i.Product.BrandId.Value : 0,
                BrandName = i.Product != null && i.Product.Brand != null ? i.Product.Brand.BrandName : "No Brand"
            })
            .Select(g => new
            {
                g.Key.BrandId,
                g.Key.BrandName,
                Quantity = g.Sum(i => i.Quantity),
                Amount = g.Sum(i => i.Amount),
                Count = g.Select(i => i.SalesInvoiceId).Distinct().Count()
            })
            .OrderByDescending(g => g.Amount)
            .ToListAsync(ct);

        return grouped.Select(g => new SalesByEntityDto
        {
            EntityId = g.BrandId,
            EntityName = g.BrandName,
            Quantity = g.Quantity,
            Amount = g.Amount,
            InvoiceCount = g.Count,
            Percentage = totalSales > 0 ? Math.Round((g.Amount / totalSales) * 100m, 2) : 0m
        }).ToList();
    }

    public async Task<PeriodComparisonResultDto> GetSalesComparisonAsync(
        DateTime currentFrom, DateTime currentTo, 
        DateTime prevFrom, DateTime prevTo, 
        CancellationToken ct = default)
    {
        var currentSummary = await GetSalesSummaryAsync(currentFrom, currentTo, ct: ct);
        var prevSummary = await GetSalesSummaryAsync(prevFrom, prevTo, ct: ct);

        var result = new PeriodComparisonResultDto
        {
            MetricName = "Total Sales (Revenue)",
            Period1Label = $"{currentFrom:dd MMM yyyy} - {currentTo:dd MMM yyyy}",
            Period2Label = $"{prevFrom:dd MMM yyyy} - {prevTo:dd MMM yyyy}",
            Period1Value = currentSummary.TotalSales,
            Period2Value = prevSummary.TotalSales,
            Details = new List<PeriodComparisonItemDto>
            {
                new() { Label = "Total Invoices", CurrentValue = currentSummary.TotalInvoices, PreviousValue = prevSummary.TotalInvoices },
                new() { Label = "Gross Sales", CurrentValue = currentSummary.GrossSales, PreviousValue = prevSummary.GrossSales },
                new() { Label = "Taxes", CurrentValue = currentSummary.TotalTaxes, PreviousValue = prevSummary.TotalTaxes },
                new() { Label = "Discounts", CurrentValue = currentSummary.TotalDiscounts, PreviousValue = prevSummary.TotalDiscounts },
                new() { Label = "Sales Returns", CurrentValue = currentSummary.TotalReturns, PreviousValue = prevSummary.TotalReturns },
                new() { Label = "Net Sales", CurrentValue = currentSummary.NetSales, PreviousValue = prevSummary.NetSales },
                new() { Label = "Quantity Sold", CurrentValue = currentSummary.TotalQuantitySold, PreviousValue = prevSummary.TotalQuantitySold }
            }
        };

        return result;
    }

    public async Task<List<UnpaidInvoiceDto>> GetUnpaidSalesInvoicesAsync(int? customerId = null, int limit = 20, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var query = _context.SalesInvoices.AsNoTracking()
            .Include(s => s.Customer)
            .Where(s => s.IsActive && s.Status != "Cancelled" && s.BalanceAmount > 0);

        if (compId > 0)
            query = query.Where(s => s.CompanyId == compId);

        if (customerId.HasValue && customerId.Value > 0)
            query = query.Where(s => s.CustomerId == customerId.Value);

        var invoices = await query
            .OrderByDescending(s => s.BalanceAmount)
            .Take(limit)
            .ToListAsync(ct);

        return invoices.Select(s => new UnpaidInvoiceDto
        {
            InvoiceId = s.Id,
            InvoiceNumber = s.InvoiceNumber,
            InvoiceDate = s.InvoiceDate,
            DueDate = s.DueDate,
            EntityId = s.CustomerId,
            EntityName = s.Customer?.CustomerName ?? "Unknown",
            GrandTotal = s.GrandTotal,
            PaidAmount = s.PaidAmount,
            BalanceAmount = s.BalanceAmount,
            Status = s.Status
        }).ToList();
    }

    public async Task<List<OutstandingBalanceDto>> GetCustomerOutstandingBalancesAsync(int limit = 20, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var customersQuery = _context.Customers.AsNoTracking().Where(c => c.IsActive);
        if (compId > 0)
            customersQuery = customersQuery.Where(c => c.CompanyId == compId);

        var customers = await customersQuery.ToListAsync(ct);

        var invoicesQuery = _context.SalesInvoices.AsNoTracking()
            .Where(s => s.IsActive && s.Status != "Cancelled" && s.BalanceAmount > 0);
        if (compId > 0)
            invoicesQuery = invoicesQuery.Where(s => s.CompanyId == compId);

        var groupedInvoices = await invoicesQuery
            .GroupBy(s => s.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                OutstandingSum = g.Sum(s => s.BalanceAmount),
                InvoicedTotal = g.Sum(s => s.GrandTotal),
                PaidTotal = g.Sum(s => s.PaidAmount),
                Count = g.Count()
            })
            .ToDictionaryAsync(g => g.CustomerId, ct);

        var result = new List<OutstandingBalanceDto>();
        foreach (var c in customers)
        {
            var hasInvoices = groupedInvoices.TryGetValue(c.Id, out var inv);
            var invOutstanding = hasInvoices ? inv!.OutstandingSum : 0m;
            var openingDr = c.BalanceType == "Dr" ? c.OpeningBalance : -c.OpeningBalance;
            var totalOutstanding = invOutstanding + openingDr;

            if (totalOutstanding > 0 || invOutstanding > 0)
            {
                result.Add(new OutstandingBalanceDto
                {
                    EntityId = c.Id,
                    EntityCode = c.CustomerCode,
                    EntityName = c.CustomerName,
                    Mobile = c.MobileNumber,
                    OpeningBalance = c.OpeningBalance,
                    BalanceType = c.BalanceType,
                    InvoicedTotal = hasInvoices ? inv!.InvoicedTotal : 0m,
                    PaidTotal = hasInvoices ? inv!.PaidTotal : 0m,
                    OutstandingAmount = totalOutstanding,
                    OverdueInvoiceCount = hasInvoices ? inv!.Count : 0,
                    CreditDays = c.CreditDays,
                    CreditLimit = c.CreditLimit
                });
            }
        }

        return result.OrderByDescending(r => r.OutstandingAmount).Take(limit).ToList();
    }

    public async Task<List<UnpaidInvoiceDto>> GetCustomerSalesHistoryAsync(int customerId, int limit = 20, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var query = _context.SalesInvoices.AsNoTracking()
            .Include(s => s.Customer)
            .Where(s => s.IsActive && s.CustomerId == customerId);
        if (compId > 0)
            query = query.Where(s => s.CompanyId == compId);

        var invoices = await query
            .OrderByDescending(s => s.InvoiceDate)
            .Take(limit)
            .ToListAsync(ct);

        return invoices.Select(s => new UnpaidInvoiceDto
        {
            InvoiceId = s.Id,
            InvoiceNumber = s.InvoiceNumber,
            InvoiceDate = s.InvoiceDate,
            DueDate = s.DueDate,
            EntityId = s.CustomerId,
            EntityName = s.Customer?.CustomerName ?? "Unknown",
            GrandTotal = s.GrandTotal,
            PaidAmount = s.PaidAmount,
            BalanceAmount = s.BalanceAmount,
            Status = s.Status
        }).ToList();
    }

    #endregion

    #region Purchase Reports

    public async Task<PurchaseSummaryResultDto> GetPurchasesSummaryAsync(
        DateTime fromDate, 
        DateTime toDate, 
        int? supplierId = null, 
        int? categoryId = null, 
        int? brandId = null, 
        int? productId = null, 
        CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.PurchaseInvoices.AsNoTracking()
            .Where(p => p.IsActive && p.Status != "Cancelled" && p.InvoiceDate >= from && p.InvoiceDate <= to);

        if (compId > 0)
            query = query.Where(p => p.CompanyId == compId);

        if (supplierId.HasValue && supplierId.Value > 0)
            query = query.Where(p => p.SupplierId == supplierId.Value);

        if (productId.HasValue || categoryId.HasValue || brandId.HasValue)
        {
            query = query.Where(p => p.Items.Any(i => 
                (!productId.HasValue || i.ProductId == productId.Value) &&
                (!categoryId.HasValue || (i.Product != null && i.Product.CategoryId == categoryId.Value)) &&
                (!brandId.HasValue || (i.Product != null && i.Product.BrandId == brandId.Value))));
        }

        var invoices = await query.ToListAsync(ct);

        var returnQuery = _context.PurchaseReturns.AsNoTracking()
            .Where(r => r.IsActive && r.Status != "Cancelled" && r.ReturnDate >= from && r.ReturnDate <= to);
        if (compId > 0)
            returnQuery = returnQuery.Where(r => r.CompanyId == compId);
        if (supplierId.HasValue && supplierId.Value > 0)
            returnQuery = returnQuery.Where(r => r.SupplierId == supplierId.Value);

        var returns = await returnQuery.ToListAsync(ct);

        var itemsQuery = _context.PurchaseInvoiceItems.AsNoTracking()
            .Where(i => i.PurchaseInvoice != null && i.PurchaseInvoice.IsActive && i.PurchaseInvoice.Status != "Cancelled" && 
                        i.PurchaseInvoice.InvoiceDate >= from && i.PurchaseInvoice.InvoiceDate <= to);
        if (compId > 0)
            itemsQuery = itemsQuery.Where(i => i.PurchaseInvoice!.CompanyId == compId);
        if (supplierId.HasValue && supplierId.Value > 0)
            itemsQuery = itemsQuery.Where(i => i.PurchaseInvoice!.SupplierId == supplierId.Value);
        if (productId.HasValue && productId.Value > 0)
            itemsQuery = itemsQuery.Where(i => i.ProductId == productId.Value);

        var totalQty = await itemsQuery.SumAsync(i => (decimal?)i.Quantity, ct) ?? 0m;

        var result = new PurchaseSummaryResultDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalInvoices = invoices.Count,
            GrossPurchases = invoices.Sum(p => p.SubTotal),
            TotalDiscounts = invoices.Sum(p => p.DiscountAmount),
            TotalTaxes = invoices.Sum(p => p.TaxAmount),
            TotalPurchases = invoices.Sum(p => p.GrandTotal),
            TotalReturns = returns.Sum(r => r.GrandTotal),
            PaidAmount = invoices.Sum(p => p.PaidAmount),
            BalanceAmount = invoices.Sum(p => p.BalanceAmount),
            TotalQuantityPurchased = totalQty
        };

        return result;
    }

    public async Task<List<PurchaseByEntityDto>> GetPurchasesBySupplierAsync(DateTime fromDate, DateTime toDate, int limit = 10, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.PurchaseInvoices.AsNoTracking()
            .Where(p => p.IsActive && p.Status != "Cancelled" && p.InvoiceDate >= from && p.InvoiceDate <= to);
        if (compId > 0)
            query = query.Where(p => p.CompanyId == compId);

        var totalPurchases = await query.SumAsync(p => (decimal?)p.GrandTotal, ct) ?? 0m;

        var grouped = await query
            .GroupBy(p => new { p.SupplierId, Name = p.Supplier != null ? p.Supplier.SupplierName : "Unknown", Code = p.Supplier != null ? p.Supplier.SupplierCode : "" })
            .Select(g => new
            {
                g.Key.SupplierId,
                g.Key.Name,
                g.Key.Code,
                TotalAmount = g.Sum(p => p.GrandTotal),
                Count = g.Count()
            })
            .OrderByDescending(g => g.TotalAmount)
            .Take(limit)
            .ToListAsync(ct);

        return grouped.Select(g => new PurchaseByEntityDto
        {
            EntityId = g.SupplierId,
            EntityCode = g.Code,
            EntityName = g.Name,
            Amount = g.TotalAmount,
            InvoiceCount = g.Count,
            Percentage = totalPurchases > 0 ? Math.Round((g.TotalAmount / totalPurchases) * 100m, 2) : 0m
        }).ToList();
    }

    public async Task<List<PurchaseByEntityDto>> GetPurchasesByProductAsync(DateTime fromDate, DateTime toDate, int limit = 10, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.PurchaseInvoiceItems.AsNoTracking()
            .Where(i => i.PurchaseInvoice != null && i.PurchaseInvoice.IsActive && i.PurchaseInvoice.Status != "Cancelled" &&
                        i.PurchaseInvoice.InvoiceDate >= from && i.PurchaseInvoice.InvoiceDate <= to);
        if (compId > 0)
            query = query.Where(i => i.PurchaseInvoice!.CompanyId == compId);

        var totalPurchases = await query.SumAsync(i => (decimal?)i.Amount, ct) ?? 0m;

        var grouped = await query
            .GroupBy(i => new { i.ProductId, Name = i.Product != null ? i.Product.ProductName : i.ProductName, Code = i.Product != null ? i.Product.ProductCode : "" })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                g.Key.Code,
                Quantity = g.Sum(i => i.Quantity),
                Amount = g.Sum(i => i.Amount),
                Count = g.Select(i => i.PurchaseInvoiceId).Distinct().Count()
            })
            .OrderByDescending(g => g.Amount)
            .Take(limit)
            .ToListAsync(ct);

        return grouped.Select(g => new PurchaseByEntityDto
        {
            EntityId = g.ProductId,
            EntityCode = g.Code,
            EntityName = g.Name,
            Quantity = g.Quantity,
            Amount = g.Amount,
            InvoiceCount = g.Count,
            Percentage = totalPurchases > 0 ? Math.Round((g.Amount / totalPurchases) * 100m, 2) : 0m
        }).ToList();
    }

    public async Task<List<PurchaseByEntityDto>> GetPurchasesByCategoryAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.PurchaseInvoiceItems.AsNoTracking()
            .Where(i => i.PurchaseInvoice != null && i.PurchaseInvoice.IsActive && i.PurchaseInvoice.Status != "Cancelled" &&
                        i.PurchaseInvoice.InvoiceDate >= from && i.PurchaseInvoice.InvoiceDate <= to);
        if (compId > 0)
            query = query.Where(i => i.PurchaseInvoice!.CompanyId == compId);

        var totalPurchases = await query.SumAsync(i => (decimal?)i.Amount, ct) ?? 0m;

        var grouped = await query
            .GroupBy(i => new 
            { 
                CategoryId = i.Product != null && i.Product.CategoryId.HasValue ? i.Product.CategoryId.Value : 0,
                CategoryName = i.Product != null && i.Product.Category != null ? i.Product.Category.CategoryName : "Uncategorized"
            })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.CategoryName,
                Quantity = g.Sum(i => i.Quantity),
                Amount = g.Sum(i => i.Amount),
                Count = g.Select(i => i.PurchaseInvoiceId).Distinct().Count()
            })
            .OrderByDescending(g => g.Amount)
            .ToListAsync(ct);

        return grouped.Select(g => new PurchaseByEntityDto
        {
            EntityId = g.CategoryId,
            EntityName = g.CategoryName,
            Quantity = g.Quantity,
            Amount = g.Amount,
            InvoiceCount = g.Count,
            Percentage = totalPurchases > 0 ? Math.Round((g.Amount / totalPurchases) * 100m, 2) : 0m
        }).ToList();
    }

    public async Task<List<PurchaseByEntityDto>> GetPurchasesByBrandAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.PurchaseInvoiceItems.AsNoTracking()
            .Where(i => i.PurchaseInvoice != null && i.PurchaseInvoice.IsActive && i.PurchaseInvoice.Status != "Cancelled" &&
                        i.PurchaseInvoice.InvoiceDate >= from && i.PurchaseInvoice.InvoiceDate <= to);
        if (compId > 0)
            query = query.Where(i => i.PurchaseInvoice!.CompanyId == compId);

        var totalPurchases = await query.SumAsync(i => (decimal?)i.Amount, ct) ?? 0m;

        var grouped = await query
            .GroupBy(i => new 
            { 
                BrandId = i.Product != null && i.Product.BrandId.HasValue ? i.Product.BrandId.Value : 0,
                BrandName = i.Product != null && i.Product.Brand != null ? i.Product.Brand.BrandName : "No Brand"
            })
            .Select(g => new
            {
                g.Key.BrandId,
                g.Key.BrandName,
                Quantity = g.Sum(i => i.Quantity),
                Amount = g.Sum(i => i.Amount),
                Count = g.Select(i => i.PurchaseInvoiceId).Distinct().Count()
            })
            .OrderByDescending(g => g.Amount)
            .ToListAsync(ct);

        return grouped.Select(g => new PurchaseByEntityDto
        {
            EntityId = g.BrandId,
            EntityName = g.BrandName,
            Quantity = g.Quantity,
            Amount = g.Amount,
            InvoiceCount = g.Count,
            Percentage = totalPurchases > 0 ? Math.Round((g.Amount / totalPurchases) * 100m, 2) : 0m
        }).ToList();
    }

    public async Task<PeriodComparisonResultDto> GetPurchasesComparisonAsync(
        DateTime currentFrom, DateTime currentTo, 
        DateTime prevFrom, DateTime prevTo, 
        CancellationToken ct = default)
    {
        var currentSummary = await GetPurchasesSummaryAsync(currentFrom, currentTo, ct: ct);
        var prevSummary = await GetPurchasesSummaryAsync(prevFrom, prevTo, ct: ct);

        var result = new PeriodComparisonResultDto
        {
            MetricName = "Total Purchases",
            Period1Label = $"{currentFrom:dd MMM yyyy} - {currentTo:dd MMM yyyy}",
            Period2Label = $"{prevFrom:dd MMM yyyy} - {prevTo:dd MMM yyyy}",
            Period1Value = currentSummary.TotalPurchases,
            Period2Value = prevSummary.TotalPurchases,
            Details = new List<PeriodComparisonItemDto>
            {
                new() { Label = "Total Invoices", CurrentValue = currentSummary.TotalInvoices, PreviousValue = prevSummary.TotalInvoices },
                new() { Label = "Gross Purchases", CurrentValue = currentSummary.GrossPurchases, PreviousValue = prevSummary.GrossPurchases },
                new() { Label = "Taxes", CurrentValue = currentSummary.TotalTaxes, PreviousValue = prevSummary.TotalTaxes },
                new() { Label = "Discounts", CurrentValue = currentSummary.TotalDiscounts, PreviousValue = prevSummary.TotalDiscounts },
                new() { Label = "Purchase Returns", CurrentValue = currentSummary.TotalReturns, PreviousValue = prevSummary.TotalReturns },
                new() { Label = "Net Purchases", CurrentValue = currentSummary.NetPurchases, PreviousValue = prevSummary.NetPurchases },
                new() { Label = "Quantity Purchased", CurrentValue = currentSummary.TotalQuantityPurchased, PreviousValue = prevSummary.TotalQuantityPurchased }
            }
        };

        return result;
    }

    public async Task<List<UnpaidInvoiceDto>> GetOutstandingSupplierPaymentsAsync(int? supplierId = null, int limit = 20, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var query = _context.PurchaseInvoices.AsNoTracking()
            .Include(p => p.Supplier)
            .Where(p => p.IsActive && p.Status != "Cancelled" && p.BalanceAmount > 0);

        if (compId > 0)
            query = query.Where(p => p.CompanyId == compId);

        if (supplierId.HasValue && supplierId.Value > 0)
            query = query.Where(p => p.SupplierId == supplierId.Value);

        var invoices = await query
            .OrderByDescending(p => p.BalanceAmount)
            .Take(limit)
            .ToListAsync(ct);

        return invoices.Select(p => new UnpaidInvoiceDto
        {
            InvoiceId = p.Id,
            InvoiceNumber = p.InvoiceNumber,
            InvoiceDate = p.InvoiceDate,
            DueDate = p.DueDate,
            EntityId = p.SupplierId,
            EntityName = p.Supplier?.SupplierName ?? "Unknown",
            GrandTotal = p.GrandTotal,
            PaidAmount = p.PaidAmount,
            BalanceAmount = p.BalanceAmount,
            Status = p.Status
        }).ToList();
    }

    public async Task<List<OutstandingBalanceDto>> GetSupplierOutstandingBalancesAsync(int limit = 20, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var suppliersQuery = _context.Suppliers.AsNoTracking().Where(s => s.IsActive);
        if (compId > 0)
            suppliersQuery = suppliersQuery.Where(s => s.CompanyId == compId);

        var suppliers = await suppliersQuery.ToListAsync(ct);

        var invoicesQuery = _context.PurchaseInvoices.AsNoTracking()
            .Where(p => p.IsActive && p.Status != "Cancelled" && p.BalanceAmount > 0);
        if (compId > 0)
            invoicesQuery = invoicesQuery.Where(p => p.CompanyId == compId);

        var groupedInvoices = await invoicesQuery
            .GroupBy(p => p.SupplierId)
            .Select(g => new
            {
                SupplierId = g.Key,
                OutstandingSum = g.Sum(p => p.BalanceAmount),
                InvoicedTotal = g.Sum(p => p.GrandTotal),
                PaidTotal = g.Sum(p => p.PaidAmount),
                Count = g.Count()
            })
            .ToDictionaryAsync(g => g.SupplierId, ct);

        var result = new List<OutstandingBalanceDto>();
        foreach (var s in suppliers)
        {
            var hasInvoices = groupedInvoices.TryGetValue(s.Id, out var inv);
            var invOutstanding = hasInvoices ? inv!.OutstandingSum : 0m;
            var openingCr = s.BalanceType == "Cr" ? s.OpeningBalance : -s.OpeningBalance;
            var totalOutstanding = invOutstanding + openingCr;

            if (totalOutstanding > 0 || invOutstanding > 0)
            {
                result.Add(new OutstandingBalanceDto
                {
                    EntityId = s.Id,
                    EntityCode = s.SupplierCode,
                    EntityName = s.SupplierName,
                    Mobile = s.Mobile,
                    OpeningBalance = s.OpeningBalance,
                    BalanceType = s.BalanceType,
                    InvoicedTotal = hasInvoices ? inv!.InvoicedTotal : 0m,
                    PaidTotal = hasInvoices ? inv!.PaidTotal : 0m,
                    OutstandingAmount = totalOutstanding,
                    OverdueInvoiceCount = hasInvoices ? inv!.Count : 0,
                    CreditDays = s.CreditDays,
                    CreditLimit = s.CreditLimit
                });
            }
        }

        return result.OrderByDescending(r => r.OutstandingAmount).Take(limit).ToList();
    }

    public async Task<List<UnpaidInvoiceDto>> GetSupplierPurchaseHistoryAsync(int supplierId, int limit = 20, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var query = _context.PurchaseInvoices.AsNoTracking()
            .Include(p => p.Supplier)
            .Where(p => p.IsActive && p.SupplierId == supplierId);
        if (compId > 0)
            query = query.Where(p => p.CompanyId == compId);

        var invoices = await query
            .OrderByDescending(p => p.InvoiceDate)
            .Take(limit)
            .ToListAsync(ct);

        return invoices.Select(p => new UnpaidInvoiceDto
        {
            InvoiceId = p.Id,
            InvoiceNumber = p.InvoiceNumber,
            InvoiceDate = p.InvoiceDate,
            DueDate = p.DueDate,
            EntityId = p.SupplierId,
            EntityName = p.Supplier?.SupplierName ?? "Unknown",
            GrandTotal = p.GrandTotal,
            PaidAmount = p.PaidAmount,
            BalanceAmount = p.BalanceAmount,
            Status = p.Status
        }).ToList();
    }

    #endregion

    #region Inventory Reports

    public async Task<InventorySummaryResultDto> GetStockSummaryAsync(int? categoryId = null, int? brandId = null, int? warehouseId = null, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var query = _context.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.Warehouse)
            .Where(p => p.IsActive);

        if (compId > 0)
            query = query.Where(p => p.CompanyId == compId);

        if (categoryId.HasValue && categoryId.Value > 0)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (brandId.HasValue && brandId.Value > 0)
            query = query.Where(p => p.BrandId == brandId.Value);

        if (warehouseId.HasValue && warehouseId.Value > 0)
            query = query.Where(p => p.WarehouseId == warehouseId.Value);

        var products = await query.ToListAsync(ct);

        var result = new InventorySummaryResultDto
        {
            TotalProducts = products.Count,
            TotalStockQuantity = products.Sum(p => p.CurrentStock),
            TotalStockValuation = products.Sum(p => p.CurrentStock * p.PurchasePrice),
            InStockCount = products.Count(p => p.CurrentStock > p.ReorderLevel),
            LowStockCount = products.Count(p => p.CurrentStock > 0 && p.CurrentStock <= p.ReorderLevel),
            OutOfStockCount = products.Count(p => p.CurrentStock == 0),
            NegativeStockCount = products.Count(p => p.CurrentStock < 0)
        };

        result.Items = products.Select(p => new ProductStockItemDto
        {
            ProductId = p.Id,
            ProductCode = p.ProductCode,
            ProductName = p.ProductName,
            CategoryName = p.Category?.CategoryName ?? "Uncategorized",
            BrandName = p.Brand?.BrandName ?? "No Brand",
            UnitName = p.Unit?.UnitName ?? "Pcs",
            WarehouseName = p.Warehouse?.WarehouseName ?? "Main",
            CurrentStock = p.CurrentStock,
            PurchasePrice = p.PurchasePrice,
            SalesPrice = p.SalesPrice,
            ReorderLevel = p.ReorderLevel,
            MinimumStock = p.MinimumStock,
            MaximumStock = p.MaximumStock,
            StockStatus = p.CurrentStock < 0 ? "Negative Stock" :
                          p.CurrentStock == 0 ? "Out of Stock" :
                          p.CurrentStock <= p.ReorderLevel ? "Low Stock" : "Optimal",
            SuggestedReorderQty = p.CurrentStock <= p.ReorderLevel 
                ? (p.MaximumStock > p.CurrentStock ? p.MaximumStock - p.CurrentStock : Math.Max(0, (p.ReorderLevel * 2) - p.CurrentStock))
                : 0m
        }).ToList();

        return result;
    }

    public async Task<List<ProductStockItemDto>> GetLowAndOutOfStockProductsAsync(bool outOfStockOnly = false, int limit = 50, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var query = _context.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Where(p => p.IsActive);

        if (compId > 0)
            query = query.Where(p => p.CompanyId == compId);

        if (outOfStockOnly)
        {
            query = query.Where(p => p.CurrentStock <= 0);
        }
        else
        {
            query = query.Where(p => p.CurrentStock <= p.ReorderLevel);
        }

        var products = await query
            .OrderBy(p => p.CurrentStock)
            .Take(limit)
            .ToListAsync(ct);

        return products.Select(p => new ProductStockItemDto
        {
            ProductId = p.Id,
            ProductCode = p.ProductCode,
            ProductName = p.ProductName,
            CategoryName = p.Category?.CategoryName ?? "Uncategorized",
            BrandName = p.Brand?.BrandName ?? "No Brand",
            UnitName = p.Unit?.UnitName ?? "Pcs",
            CurrentStock = p.CurrentStock,
            PurchasePrice = p.PurchasePrice,
            SalesPrice = p.SalesPrice,
            ReorderLevel = p.ReorderLevel,
            MinimumStock = p.MinimumStock,
            MaximumStock = p.MaximumStock,
            StockStatus = p.CurrentStock <= 0 ? "Out of Stock" : "Low Stock",
            SuggestedReorderQty = p.MaximumStock > p.CurrentStock 
                ? (p.MaximumStock - p.CurrentStock) 
                : Math.Max(10m, (p.ReorderLevel * 2) - p.CurrentStock)
        }).ToList();
    }

    public async Task<List<ProductStockItemDto>> GetStockValuationByCategoryAsync(CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var query = _context.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive);
        if (compId > 0)
            query = query.Where(p => p.CompanyId == compId);

        var list = await query.ToListAsync(ct);

        var grouped = list
            .GroupBy(p => p.Category?.CategoryName ?? "Uncategorized")
            .Select(g => new ProductStockItemDto
            {
                CategoryName = g.Key,
                CurrentStock = g.Sum(p => p.CurrentStock),
                PurchasePrice = g.Count(), // Product count reused
                SalesPrice = 0,
                SuggestedReorderQty = g.Sum(p => p.CurrentStock * p.PurchasePrice) // Valuation stored here
            })
            .OrderByDescending(g => g.SuggestedReorderQty)
            .ToList();

        return grouped;
    }

    public async Task<List<ProductStockItemDto>> GetStockValuationByBrandAsync(CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var query = _context.Products.AsNoTracking()
            .Include(p => p.Brand)
            .Where(p => p.IsActive);
        if (compId > 0)
            query = query.Where(p => p.CompanyId == compId);

        var list = await query.ToListAsync(ct);

        var grouped = list
            .GroupBy(p => p.Brand?.BrandName ?? "No Brand")
            .Select(g => new ProductStockItemDto
            {
                BrandName = g.Key,
                CurrentStock = g.Sum(p => p.CurrentStock),
                PurchasePrice = g.Count(),
                SuggestedReorderQty = g.Sum(p => p.CurrentStock * p.PurchasePrice)
            })
            .OrderByDescending(g => g.SuggestedReorderQty)
            .ToList();

        return grouped;
    }

    public async Task<List<ProductStockItemDto>> GetStockValuationByWarehouseAsync(CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var query = _context.Products.AsNoTracking()
            .Include(p => p.Warehouse)
            .Where(p => p.IsActive);
        if (compId > 0)
            query = query.Where(p => p.CompanyId == compId);

        var list = await query.ToListAsync(ct);

        var grouped = list
            .GroupBy(p => p.Warehouse?.WarehouseName ?? "Default Warehouse")
            .Select(g => new ProductStockItemDto
            {
                WarehouseName = g.Key,
                CurrentStock = g.Sum(p => p.CurrentStock),
                PurchasePrice = g.Count(),
                SuggestedReorderQty = g.Sum(p => p.CurrentStock * p.PurchasePrice)
            })
            .OrderByDescending(g => g.SuggestedReorderQty)
            .ToList();

        return grouped;
    }

    public async Task<List<ProductStockItemDto>> GetProductsWithoutSalesAsync(int days = 90, int limit = 50, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var cutoffDate = DateTime.Today.AddDays(-days);

        // Find product IDs sold in recent window
        var soldProductIdsQuery = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= cutoffDate);
        if (compId > 0)
            soldProductIdsQuery = soldProductIdsQuery.Where(i => i.SalesInvoice!.CompanyId == compId);

        var soldProductIds = await soldProductIdsQuery
            .Select(i => i.ProductId)
            .Distinct()
            .ToListAsync(ct);

        var query = _context.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive && !soldProductIds.Contains(p.Id));

        if (compId > 0)
            query = query.Where(p => p.CompanyId == compId);

        var deadStock = await query
            .OrderByDescending(p => p.CurrentStock * p.PurchasePrice)
            .Take(limit)
            .ToListAsync(ct);

        return deadStock.Select(p => new ProductStockItemDto
        {
            ProductId = p.Id,
            ProductCode = p.ProductCode,
            ProductName = p.ProductName,
            CategoryName = p.Category?.CategoryName ?? "Uncategorized",
            BrandName = p.Brand?.BrandName ?? "No Brand",
            CurrentStock = p.CurrentStock,
            PurchasePrice = p.PurchasePrice,
            SalesPrice = p.SalesPrice,
            StockStatus = $"No sales in {days}+ days"
        }).ToList();
    }

    public async Task<List<ProductStockItemDto>> GetNegativeStockProductsAsync(CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var query = _context.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Where(p => p.IsActive && p.CurrentStock < 0);

        if (compId > 0)
            query = query.Where(p => p.CompanyId == compId);

        var prods = await query.ToListAsync(ct);

        return prods.Select(p => new ProductStockItemDto
        {
            ProductId = p.Id,
            ProductCode = p.ProductCode,
            ProductName = p.ProductName,
            CategoryName = p.Category?.CategoryName ?? "Uncategorized",
            UnitName = p.Unit?.UnitName ?? "Pcs",
            CurrentStock = p.CurrentStock,
            PurchasePrice = p.PurchasePrice,
            StockStatus = "Negative Stock"
        }).ToList();
    }

    public async Task<List<ProductStockItemDto>> GetReorderSuggestionsAsync(int limit = 50, CancellationToken ct = default)
    {
        return await GetLowAndOutOfStockProductsAsync(false, limit, ct);
    }

    public async Task<List<DailyTrendDto>> GetProductStockMovementHistoryAsync(int productId, int limit = 20, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var query = _context.StockTransactions.AsNoTracking()
            .Where(t => t.IsActive && t.ProductId == productId);

        if (compId > 0)
            query = query.Where(t => t.CompanyId == compId);

        var txs = await query
            .OrderByDescending(t => t.TransactionDate)
            .Take(limit)
            .ToListAsync(ct);

        return txs.Select(t => new DailyTrendDto
        {
            Date = t.TransactionDate,
            DateLabel = $"{t.TransactionDate:yyyy-MM-dd} ({t.TransactionType})",
            SalesAmount = t.Quantity,
            PurchaseAmount = t.Rate
        }).ToList();
    }

    #endregion

    #region Aging Analysis

    public async Task<AgingReportResultDto> GetReceivablesAgingAsync(CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var today = DateTime.Today;

        var query = _context.SalesInvoices.AsNoTracking()
            .Include(s => s.Customer)
            .Where(s => s.IsActive && s.Status != "Cancelled" && s.BalanceAmount > 0);

        if (compId > 0)
            query = query.Where(s => s.CompanyId == compId);

        var invoices = await query.ToListAsync(ct);

        var result = new AgingReportResultDto { ReportType = "Receivables (Customers)" };

        var grouped = invoices.GroupBy(s => new { s.CustomerId, Name = s.Customer?.CustomerName ?? "Unknown" });

        foreach (var g in grouped)
        {
            var row = new AgingEntityRowDto
            {
                EntityId = g.Key.CustomerId,
                EntityName = g.Key.Name
            };

            foreach (var inv in g)
            {
                var refDate = inv.DueDate ?? inv.InvoiceDate;
                var days = (today - refDate.Date).TotalDays;

                if (days <= 30)
                {
                    row.Current0To30 += inv.BalanceAmount;
                    result.CurrentDaysTotal += inv.BalanceAmount;
                }
                else if (days <= 60)
                {
                    row.Days31To60 += inv.BalanceAmount;
                    result.Days31To60Total += inv.BalanceAmount;
                }
                else if (days <= 90)
                {
                    row.Days61To90 += inv.BalanceAmount;
                    result.Days61To90Total += inv.BalanceAmount;
                }
                else
                {
                    row.Above90Days += inv.BalanceAmount;
                    result.Above90DaysTotal += inv.BalanceAmount;
                }

                row.TotalBalance += inv.BalanceAmount;
            }

            result.Rows.Add(row);
        }

        result.Rows = result.Rows.OrderByDescending(r => r.TotalBalance).ToList();
        return result;
    }

    public async Task<AgingReportResultDto> GetPayablesAgingAsync(CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var today = DateTime.Today;

        var query = _context.PurchaseInvoices.AsNoTracking()
            .Include(p => p.Supplier)
            .Where(p => p.IsActive && p.Status != "Cancelled" && p.BalanceAmount > 0);

        if (compId > 0)
            query = query.Where(p => p.CompanyId == compId);

        var invoices = await query.ToListAsync(ct);

        var result = new AgingReportResultDto { ReportType = "Payables (Suppliers)" };

        var grouped = invoices.GroupBy(p => new { p.SupplierId, Name = p.Supplier?.SupplierName ?? "Unknown" });

        foreach (var g in grouped)
        {
            var row = new AgingEntityRowDto
            {
                EntityId = g.Key.SupplierId,
                EntityName = g.Key.Name
            };

            foreach (var inv in g)
            {
                var refDate = inv.DueDate ?? inv.InvoiceDate;
                var days = (today - refDate.Date).TotalDays;

                if (days <= 30)
                {
                    row.Current0To30 += inv.BalanceAmount;
                    result.CurrentDaysTotal += inv.BalanceAmount;
                }
                else if (days <= 60)
                {
                    row.Days31To60 += inv.BalanceAmount;
                    result.Days31To60Total += inv.BalanceAmount;
                }
                else if (days <= 90)
                {
                    row.Days61To90 += inv.BalanceAmount;
                    result.Days61To90Total += inv.BalanceAmount;
                }
                else
                {
                    row.Above90Days += inv.BalanceAmount;
                    result.Above90DaysTotal += inv.BalanceAmount;
                }

                row.TotalBalance += inv.BalanceAmount;
            }

            result.Rows.Add(row);
        }

        result.Rows = result.Rows.OrderByDescending(r => r.TotalBalance).ToList();
        return result;
    }

    #endregion

    #region Executive Business Intelligence

    public async Task<ExecutiveSnapshotDto> GetExecutiveBusinessSnapshotAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var sales = await GetSalesSummaryAsync(fromDate, toDate, ct: ct);
        var purchases = await GetPurchasesSummaryAsync(fromDate, toDate, ct: ct);
        var stock = await GetStockSummaryAsync(ct: ct);

        var receivables = await _context.SalesInvoices.AsNoTracking()
            .Where(s => s.IsActive && s.Status != "Cancelled" && s.BalanceAmount > 0 && (compId == 0 || s.CompanyId == compId))
            .SumAsync(s => (decimal?)s.BalanceAmount, ct) ?? 0m;

        var payables = await _context.PurchaseInvoices.AsNoTracking()
            .Where(p => p.IsActive && p.Status != "Cancelled" && p.BalanceAmount > 0 && (compId == 0 || p.CompanyId == compId))
            .SumAsync(p => (decimal?)p.BalanceAmount, ct) ?? 0m;

        var customerCount = await _context.Customers.AsNoTracking()
            .CountAsync(c => c.IsActive && (compId == 0 || c.CompanyId == compId), ct);

        var supplierCount = await _context.Suppliers.AsNoTracking()
            .CountAsync(s => s.IsActive && (compId == 0 || s.CompanyId == compId), ct);

        return new ExecutiveSnapshotDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalRevenue = sales.TotalSales,
            TotalSalesReturns = sales.TotalReturns,
            NetSales = sales.NetSales,
            TotalPurchases = purchases.TotalPurchases,
            TotalPurchaseReturns = purchases.TotalReturns,
            NetPurchases = purchases.NetPurchases,
            OutstandingReceivables = receivables,
            OutstandingPayables = payables,
            TotalStockValuation = stock.TotalStockValuation,
            LowStockProductCount = stock.LowStockCount,
            OutOfStockProductCount = stock.OutOfStockCount,
            TotalActiveCustomers = customerCount,
            TotalActiveSuppliers = supplierCount
        };
    }

    public async Task<List<DailyTrendDto>> GetDailySalesAndPurchasesTrendAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var salesQuery = _context.SalesInvoices.AsNoTracking()
            .Where(s => s.IsActive && s.Status != "Cancelled" && s.InvoiceDate >= from && s.InvoiceDate <= to);
        if (compId > 0)
            salesQuery = salesQuery.Where(s => s.CompanyId == compId);

        var purchasesQuery = _context.PurchaseInvoices.AsNoTracking()
            .Where(p => p.IsActive && p.Status != "Cancelled" && p.InvoiceDate >= from && p.InvoiceDate <= to);
        if (compId > 0)
            purchasesQuery = purchasesQuery.Where(p => p.CompanyId == compId);

        var dailySales = await salesQuery
            .GroupBy(s => s.InvoiceDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(s => s.GrandTotal), Count = g.Count() })
            .ToDictionaryAsync(g => g.Date, ct);

        var dailyPurchases = await purchasesQuery
            .GroupBy(p => p.InvoiceDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(p => p.GrandTotal), Count = g.Count() })
            .ToDictionaryAsync(g => g.Date, ct);

        var result = new List<DailyTrendDto>();
        var totalDays = (int)(toDate.Date - fromDate.Date).TotalDays + 1;
        if (totalDays > 60) totalDays = 60; // Cap to 60 data points

        for (int i = 0; i < totalDays; i++)
        {
            var day = fromDate.Date.AddDays(i);
            var s = dailySales.GetValueOrDefault(day);
            var p = dailyPurchases.GetValueOrDefault(day);

            result.Add(new DailyTrendDto
            {
                Date = day,
                DateLabel = day.ToString("dd MMM"),
                SalesAmount = s?.Total ?? 0m,
                PurchaseAmount = p?.Total ?? 0m,
                SalesInvoiceCount = s?.Count ?? 0,
                PurchaseInvoiceCount = p?.Count ?? 0
            });
        }

        return result;
    }

    public async Task<List<PeriodComparisonItemDto>> GetDecliningSalesProductsAsync(int days = 30, int limit = 10, CancellationToken ct = default)
    {
        var compId = RequireCompanyId();
        var currentStart = DateTime.Today.AddDays(-days);
        var currentEnd = DateTime.Today.AddDays(1).AddTicks(-1);
        var prevStart = DateTime.Today.AddDays(-days * 2);
        var prevEnd = currentStart.AddTicks(-1);

        var currentSales = await _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= currentStart && i.SalesInvoice.InvoiceDate <= currentEnd &&
                        (compId == 0 || i.SalesInvoice.CompanyId == compId))
            .GroupBy(i => new { i.ProductId, Name = i.Product != null ? i.Product.ProductName : i.ProductName })
            .Select(g => new { g.Key.ProductId, g.Key.Name, Qty = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(g => g.ProductId, ct);

        var prevSales = await _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= prevStart && i.SalesInvoice.InvoiceDate <= prevEnd &&
                        (compId == 0 || i.SalesInvoice.CompanyId == compId))
            .GroupBy(i => new { i.ProductId, Name = i.Product != null ? i.Product.ProductName : i.ProductName })
            .Select(g => new { g.Key.ProductId, g.Key.Name, Qty = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(g => g.ProductId, ct);

        var declining = new List<PeriodComparisonItemDto>();
        foreach (var p in prevSales)
        {
            var curQty = currentSales.TryGetValue(p.Key, out var c) ? c.Qty : 0m;
            if (curQty < p.Value.Qty)
            {
                declining.Add(new PeriodComparisonItemDto
                {
                    Label = p.Value.Name,
                    CurrentValue = curQty,
                    PreviousValue = p.Value.Qty
                });
            }
        }

        return declining.OrderBy(d => d.Change).Take(limit).ToList();
    }

    #endregion
}
