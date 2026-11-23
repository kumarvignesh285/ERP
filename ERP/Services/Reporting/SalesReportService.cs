using System;
using System.Collections.Generic;
using System.Globalization;
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

public class SalesReportService : ISalesReportService
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;

    public SalesReportService(AppDbContext context, ICompanyContext companyContext)
    {
        _context = context;
        _companyContext = companyContext;
    }

    private int? ActiveCompanyId => _companyContext.CurrentCompanyId;

    private IQueryable<ERP.Models.SalesInvoice> GetBaseSalesQuery(ReportFilterViewModel filter, bool includeCancelled = false)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.SalesInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.InvoiceDate >= fromDate && x.InvoiceDate <= toDate);

        if (!includeCancelled)
        {
            query = query.Where(x => x.Status != "Cancelled");
        }

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
        {
            query = query.Where(x => x.CompanyId == ActiveCompanyId.Value);
        }

        if (filter.CustomerId.HasValue && filter.CustomerId.Value > 0)
        {
            query = query.Where(x => x.CustomerId == filter.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Salesperson))
        {
            query = query.Where(x => x.Salesperson != null && x.Salesperson.ToLower().Contains(filter.Salesperson.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(x => x.Status == filter.Status);
        }

        if (filter.ProductId.HasValue || filter.CategoryId.HasValue || filter.BrandId.HasValue)
        {
            query = query.Where(x => x.Items.Any(i =>
                (!filter.ProductId.HasValue || i.ProductId == filter.ProductId.Value) &&
                (!filter.CategoryId.HasValue || (i.Product != null && i.Product.CategoryId == filter.CategoryId.Value)) &&
                (!filter.BrandId.HasValue || (i.Product != null && i.Product.BrandId == filter.BrandId.Value))));
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.InvoiceNumber.ToLower().Contains(search) ||
                (x.Customer != null && x.Customer.CustomerName.ToLower().Contains(search)) ||
                (x.Customer != null && x.Customer.CustomerCode.ToLower().Contains(search)));
        }

        return query;
    }

    public async Task<List<DailySalesDto>> GetDailySalesReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseSalesQuery(filter);

        var data = await query
            .GroupBy(x => x.InvoiceDate.Date)
            .Select(g => new DailySalesDto
            {
                Date = g.Key,
                InvoiceCount = g.Count(),
                GrossSales = g.Sum(x => x.SubTotal),
                DiscountAmount = g.Sum(x => x.DiscountAmount),
                TaxAmount = g.Sum(x => x.TaxAmount),
                NetSales = g.Sum(x => x.GrandTotal),
                PaidAmount = g.Sum(x => x.PaidAmount),
                BalanceAmount = g.Sum(x => x.BalanceAmount)
            })
            .OrderByDescending(x => x.Date)
            .ToListAsync(ct);

        return data;
    }

    public async Task<List<MonthlySalesDto>> GetMonthlySalesReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseSalesQuery(filter);

        var raw = await query
            .GroupBy(x => new { x.InvoiceDate.Year, x.InvoiceDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                InvoiceCount = g.Count(),
                GrossSales = g.Sum(x => x.SubTotal),
                TaxAmount = g.Sum(x => x.TaxAmount),
                NetSales = g.Sum(x => x.GrandTotal),
                PaidAmount = g.Sum(x => x.PaidAmount)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        var result = new List<MonthlySalesDto>();
        decimal prevNetSales = 0m;

        foreach (var item in raw)
        {
            var growth = prevNetSales > 0 ? Math.Round(((item.NetSales - prevNetSales) / prevNetSales) * 100m, 2) : 0m;
            result.Add(new MonthlySalesDto
            {
                Year = item.Year,
                Month = item.Month,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(item.Month) + " " + item.Year,
                InvoiceCount = item.InvoiceCount,
                GrossSales = item.GrossSales,
                TaxAmount = item.TaxAmount,
                NetSales = item.NetSales,
                PaidAmount = item.PaidAmount,
                GrowthRate = growth
            });
            prevNetSales = item.NetSales;
        }

        result.Reverse();
        return result;
    }

    public async Task<List<YearlySalesDto>> GetYearlySalesReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseSalesQuery(filter);

        var returnsQuery = _context.SalesReturns.AsNoTracking().Where(r => r.IsActive && r.Status != "Cancelled");
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            returnsQuery = returnsQuery.Where(r => r.CompanyId == ActiveCompanyId.Value);

        var salesByYear = await query
            .GroupBy(x => x.InvoiceDate.Year)
            .Select(g => new
            {
                Year = g.Key,
                InvoiceCount = g.Count(),
                GrossSales = g.Sum(x => x.SubTotal),
                TaxAmount = g.Sum(x => x.TaxAmount),
                NetSales = g.Sum(x => x.GrandTotal)
            })
            .OrderByDescending(x => x.Year)
            .ToListAsync(ct);

        var returnsByYear = await returnsQuery
            .GroupBy(r => r.ReturnDate.Year)
            .Select(g => new { Year = g.Key, TotalReturns = g.Sum(r => r.GrandTotal) })
            .ToDictionaryAsync(x => x.Year, x => x.TotalReturns, ct);

        return salesByYear.Select(s => new YearlySalesDto
        {
            Year = s.Year,
            FinancialYear = $"FY {s.Year}-{s.Year + 1}",
            InvoiceCount = s.InvoiceCount,
            GrossSales = s.GrossSales,
            TaxAmount = s.TaxAmount,
            NetSales = s.NetSales,
            TotalReturns = returnsByYear.TryGetValue(s.Year, out var rTot) ? rTot : 0m
        }).ToList();
    }

    public async Task<List<SalesRegisterDto>> GetSalesRegisterAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseSalesQuery(filter);

        return await query
            .Include(x => x.Customer)
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new SalesRegisterDto
            {
                InvoiceId = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                InvoiceDate = x.InvoiceDate,
                CustomerName = x.Customer != null ? x.Customer.CustomerName : "Walk-in Customer",
                GSTNumber = x.Customer != null ? x.Customer.GSTNumber : null,
                Salesperson = x.Salesperson,
                SubTotal = x.SubTotal,
                DiscountAmount = x.DiscountAmount,
                TaxAmount = x.TaxAmount,
                GrandTotal = x.GrandTotal,
                PaidAmount = x.PaidAmount,
                BalanceAmount = x.BalanceAmount,
                Status = x.Status,
                WithGST = x.WithGST
            })
            .ToListAsync(ct);
    }

    public async Task<List<ProductWiseSalesDto>> GetProductWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);

        var itemsQuery = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= fromDate && i.SalesInvoice.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            itemsQuery = itemsQuery.Where(i => i.SalesInvoice!.CompanyId == ActiveCompanyId.Value);

        if (filter.CustomerId.HasValue && filter.CustomerId.Value > 0)
            itemsQuery = itemsQuery.Where(i => i.SalesInvoice!.CustomerId == filter.CustomerId.Value);

        if (filter.ProductId.HasValue && filter.ProductId.Value > 0)
            itemsQuery = itemsQuery.Where(i => i.ProductId == filter.ProductId.Value);

        if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            itemsQuery = itemsQuery.Where(i => i.Product != null && i.Product.CategoryId == filter.CategoryId.Value);

        if (filter.BrandId.HasValue && filter.BrandId.Value > 0)
            itemsQuery = itemsQuery.Where(i => i.Product != null && i.Product.BrandId == filter.BrandId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            itemsQuery = itemsQuery.Where(i => i.ProductName.ToLower().Contains(search) || (i.Product != null && i.Product.ProductCode.ToLower().Contains(search)));
        }

        var totalRevenue = await itemsQuery.SumAsync(i => (decimal?)i.Amount, ct) ?? 0m;

        var grouped = await itemsQuery
            .GroupBy(i => new { i.ProductId, i.ProductName, i.UnitName, CategoryName = i.Product != null && i.Product.Category != null ? i.Product.Category.CategoryName : "General", BrandName = i.Product != null && i.Product.Brand != null ? i.Product.Brand.BrandName : "General", ProductCode = i.Product != null ? i.Product.ProductCode : "" })
            .Select(g => new ProductWiseSalesDto
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                CategoryName = g.Key.CategoryName,
                BrandName = g.Key.BrandName,
                UnitName = g.Key.UnitName ?? "Nos",
                QuantitySold = g.Sum(x => x.Quantity),
                TotalSalesAmount = g.Sum(x => x.Amount),
                TotalTaxAmount = g.Sum(x => x.TaxAmount),
                AverageRate = g.Sum(x => x.Quantity) > 0 ? Math.Round(g.Sum(x => x.Amount) / g.Sum(x => x.Quantity), 2) : 0,
                PercentageOfTotal = totalRevenue > 0 ? Math.Round((g.Sum(x => x.Amount) / totalRevenue) * 100m, 2) : 0m
            })
            .OrderByDescending(x => x.TotalSalesAmount)
            .ToListAsync(ct);

        return grouped;
    }

    public async Task<List<CategoryWiseSalesDto>> GetCategoryWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var productSales = await GetProductWiseSalesAsync(filter, ct);
        var totalAmount = productSales.Sum(x => x.TotalSalesAmount);

        return productSales
            .GroupBy(x => x.CategoryName)
            .Select(g => new CategoryWiseSalesDto
            {
                CategoryId = 0,
                CategoryName = g.Key,
                ProductCount = g.Select(x => x.ProductId).Distinct().Count(),
                QuantitySold = g.Sum(x => x.QuantitySold),
                TotalSalesAmount = g.Sum(x => x.TotalSalesAmount),
                PercentageOfTotal = totalAmount > 0 ? Math.Round((g.Sum(x => x.TotalSalesAmount) / totalAmount) * 100m, 2) : 0m
            })
            .OrderByDescending(x => x.TotalSalesAmount)
            .ToList();
    }

    public async Task<List<BrandWiseSalesDto>> GetBrandWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var productSales = await GetProductWiseSalesAsync(filter, ct);
        var totalAmount = productSales.Sum(x => x.TotalSalesAmount);

        return productSales
            .GroupBy(x => x.BrandName)
            .Select(g => new BrandWiseSalesDto
            {
                BrandId = 0,
                BrandName = g.Key,
                QuantitySold = g.Sum(x => x.QuantitySold),
                TotalSalesAmount = g.Sum(x => x.TotalSalesAmount),
                PercentageOfTotal = totalAmount > 0 ? Math.Round((g.Sum(x => x.TotalSalesAmount) / totalAmount) * 100m, 2) : 0m
            })
            .OrderByDescending(x => x.TotalSalesAmount)
            .ToList();
    }

    public async Task<List<CustomerWiseSalesDto>> GetCustomerWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseSalesQuery(filter);

        return await query
            .GroupBy(x => new { x.CustomerId, CustomerName = x.Customer != null ? x.Customer.CustomerName : "Walk-in Customer", CustomerCode = x.Customer != null ? x.Customer.CustomerCode : "", MobileNumber = x.Customer != null ? x.Customer.MobileNumber : "", City = x.Customer != null ? x.Customer.City : "" })
            .Select(g => new CustomerWiseSalesDto
            {
                CustomerId = g.Key.CustomerId,
                CustomerCode = g.Key.CustomerCode,
                CustomerName = g.Key.CustomerName,
                MobileNumber = g.Key.MobileNumber,
                City = g.Key.City,
                InvoiceCount = g.Count(),
                TotalSalesAmount = g.Sum(x => x.GrandTotal),
                PaidAmount = g.Sum(x => x.PaidAmount),
                BalanceAmount = g.Sum(x => x.BalanceAmount)
            })
            .OrderByDescending(x => x.TotalSalesAmount)
            .ToListAsync(ct);
    }

    public async Task<List<SalespersonWiseSalesDto>> GetSalespersonWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseSalesQuery(filter);

        return await query
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Salesperson) ? "Unassigned" : x.Salesperson)
            .Select(g => new SalespersonWiseSalesDto
            {
                Salesperson = g.Key,
                InvoiceCount = g.Count(),
                TotalSalesAmount = g.Sum(x => x.GrandTotal),
                PaidAmount = g.Sum(x => x.PaidAmount),
                OutstandingAmount = g.Sum(x => x.BalanceAmount)
            })
            .OrderByDescending(x => x.TotalSalesAmount)
            .ToListAsync(ct);
    }

    public async Task<List<SalesReturnReportDto>> GetSalesReturnReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.SalesReturns.AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.SalesInvoice)
            .Where(r => r.IsActive && r.ReturnDate >= fromDate && r.ReturnDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(r => r.CompanyId == ActiveCompanyId.Value);

        if (filter.CustomerId.HasValue && filter.CustomerId.Value > 0)
            query = query.Where(r => r.CustomerId == filter.CustomerId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(r => r.Status == filter.Status);

        return await query
            .OrderByDescending(r => r.ReturnDate)
            .Select(r => new SalesReturnReportDto
            {
                ReturnId = r.Id,
                ReturnNumber = r.ReturnNumber,
                ReturnDate = r.ReturnDate,
                CustomerName = r.Customer != null ? r.Customer.CustomerName : "Customer",
                OriginalInvoiceNumber = r.SalesInvoice != null ? r.SalesInvoice.InvoiceNumber : null,
                Reason = r.Reason,
                SubTotal = r.SubTotal,
                TaxAmount = r.TaxAmount,
                GrandTotal = r.GrandTotal,
                Status = r.Status
            })
            .ToListAsync(ct);
    }

    public async Task<List<DiscountReportDto>> GetDiscountReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseSalesQuery(filter).Where(x => x.DiscountAmount > 0);

        return await query
            .Include(x => x.Customer)
            .OrderByDescending(x => x.DiscountAmount)
            .Select(x => new DiscountReportDto
            {
                InvoiceId = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                InvoiceDate = x.InvoiceDate,
                CustomerName = x.Customer != null ? x.Customer.CustomerName : "Walk-in Customer",
                SubTotal = x.SubTotal,
                DiscountAmount = x.DiscountAmount,
                GrandTotal = x.GrandTotal
            })
            .ToListAsync(ct);
    }

    public async Task<List<PaymentCollectionReportDto>> GetPaymentCollectionReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseSalesQuery(filter);

        return await query
            .Include(x => x.Customer)
            .OrderByDescending(x => x.InvoiceDate)
            .Select(x => new PaymentCollectionReportDto
            {
                InvoiceId = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                InvoiceDate = x.InvoiceDate,
                CustomerName = x.Customer != null ? x.Customer.CustomerName : "Walk-in Customer",
                GrandTotal = x.GrandTotal,
                PaidAmount = x.PaidAmount,
                BalanceAmount = x.BalanceAmount,
                PaymentTerms = x.PaymentTerms
            })
            .ToListAsync(ct);
    }

    public async Task<List<BillDetailsReportDto>> GetBillDetailsReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);

        var query = _context.SalesInvoiceItems.AsNoTracking()
            .Include(i => i.SalesInvoice)
                .ThenInclude(s => s!.Customer)
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= fromDate && i.SalesInvoice.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.SalesInvoice!.CompanyId == ActiveCompanyId.Value);

        if (filter.CustomerId.HasValue && filter.CustomerId.Value > 0)
            query = query.Where(i => i.SalesInvoice!.CustomerId == filter.CustomerId.Value);

        if (filter.ProductId.HasValue && filter.ProductId.Value > 0)
            query = query.Where(i => i.ProductId == filter.ProductId.Value);

        return await query
            .OrderByDescending(i => i.SalesInvoice!.InvoiceDate)
            .ThenBy(i => i.SalesInvoiceId)
            .ThenBy(i => i.SortOrder)
            .Take(1000)
            .Select(i => new BillDetailsReportDto
            {
                InvoiceId = i.SalesInvoiceId,
                InvoiceNumber = i.SalesInvoice!.InvoiceNumber,
                InvoiceDate = i.SalesInvoice.InvoiceDate,
                CustomerName = i.SalesInvoice.Customer != null ? i.SalesInvoice.Customer.CustomerName : "Walk-in",
                ProductName = i.ProductName,
                HSNCode = i.HSNCode,
                Quantity = i.Quantity,
                UnitName = i.UnitName,
                Rate = i.Rate,
                DiscountAmount = i.DiscountAmount,
                TaxPercentage = i.TaxPercentage,
                TaxAmount = i.TaxAmount,
                ItemAmount = i.Amount
            })
            .ToListAsync(ct);
    }

    public async Task<List<CancelledSalesReportDto>> GetCancelledSalesReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseSalesQuery(filter, includeCancelled: true).Where(x => x.Status == "Cancelled");

        return await query
            .Include(x => x.Customer)
            .OrderByDescending(x => x.UpdatedAt ?? x.InvoiceDate)
            .Select(x => new CancelledSalesReportDto
            {
                InvoiceId = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                InvoiceDate = x.InvoiceDate,
                CustomerName = x.Customer != null ? x.Customer.CustomerName : "Customer",
                GrandTotal = x.GrandTotal,
                Notes = x.Notes,
                CreatedBy = x.CreatedBy,
                CancelledDate = x.UpdatedAt ?? x.InvoiceDate
            })
            .ToListAsync(ct);
    }

    public async Task<List<ReportSummaryCard>> GetSalesSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseSalesQuery(filter);

        var totalInvoices = await query.CountAsync(ct);
        var grossSales = await query.SumAsync(x => (decimal?)x.SubTotal, ct) ?? 0m;
        var totalTax = await query.SumAsync(x => (decimal?)x.TaxAmount, ct) ?? 0m;
        var totalDiscount = await query.SumAsync(x => (decimal?)x.DiscountAmount, ct) ?? 0m;
        var grandTotal = await query.SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0m;
        var totalPaid = await query.SumAsync(x => (decimal?)x.PaidAmount, ct) ?? 0m;
        var totalBalance = await query.SumAsync(x => (decimal?)x.BalanceAmount, ct) ?? 0m;

        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var returnQuery = _context.SalesReturns.AsNoTracking()
            .Where(r => r.IsActive && r.Status != "Cancelled" && r.ReturnDate >= fromDate && r.ReturnDate <= toDate);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            returnQuery = returnQuery.Where(r => r.CompanyId == ActiveCompanyId.Value);

        var totalReturns = await returnQuery.SumAsync(r => (decimal?)r.GrandTotal, ct) ?? 0m;
        var netSales = grandTotal - totalReturns;

        return new List<ReportSummaryCard>
        {
            new() { Title = "Total Invoices", Value = totalInvoices.ToString("N0"), Icon = "fa-file-invoice", ColorClass = "primary" },
            new() { Title = "Gross Sales", Value = $"₹{grossSales:N2}", Icon = "fa-receipt", ColorClass = "info" },
            new() { Title = "Total Tax", Value = $"₹{totalTax:N2}", Icon = "fa-percent", ColorClass = "warning" },
            new() { Title = "Net Sales (After Returns)", Value = $"₹{netSales:N2}", Icon = "fa-indian-rupee-sign", ColorClass = "success" },
            new() { Title = "Collected Amount", Value = $"₹{totalPaid:N2}", Icon = "fa-hand-holding-dollar", ColorClass = "success" },
            new() { Title = "Outstanding Receivable", Value = $"₹{totalBalance:N2}", Icon = "fa-clock", ColorClass = "danger" }
        };
    }
}
