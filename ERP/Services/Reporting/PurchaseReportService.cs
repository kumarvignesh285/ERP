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

public class PurchaseReportService : IPurchaseReportService
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;

    public PurchaseReportService(AppDbContext context, ICompanyContext companyContext)
    {
        _context = context;
        _companyContext = companyContext;
    }

    private int? ActiveCompanyId => _companyContext.CurrentCompanyId;

    private IQueryable<ERP.Models.PurchaseInvoice> GetBasePurchaseQuery(ReportFilterViewModel filter)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.PurchaseInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "Cancelled" && x.InvoiceDate >= fromDate && x.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
        {
            query = query.Where(x => x.CompanyId == ActiveCompanyId.Value);
        }

        if (filter.SupplierId.HasValue && filter.SupplierId.Value > 0)
        {
            query = query.Where(x => x.SupplierId == filter.SupplierId.Value);
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
                (x.Supplier != null && x.Supplier.SupplierName.ToLower().Contains(search)) ||
                (x.Supplier != null && x.Supplier.SupplierCode.ToLower().Contains(search)));
        }

        return query;
    }

    public async Task<List<DailyPurchaseDto>> GetDailyPurchaseReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBasePurchaseQuery(filter);

        return await query
            .GroupBy(x => x.InvoiceDate.Date)
            .Select(g => new DailyPurchaseDto
            {
                Date = g.Key,
                BillCount = g.Count(),
                GrossPurchase = g.Sum(x => x.SubTotal),
                DiscountAmount = g.Sum(x => x.DiscountAmount),
                TaxAmount = g.Sum(x => x.TaxAmount),
                NetPurchase = g.Sum(x => x.GrandTotal),
                PaidAmount = g.Sum(x => x.PaidAmount),
                BalanceAmount = g.Sum(x => x.BalanceAmount)
            })
            .OrderByDescending(x => x.Date)
            .ToListAsync(ct);
    }

    public async Task<List<MonthlyPurchaseDto>> GetMonthlyPurchaseReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBasePurchaseQuery(filter);

        return await query
            .GroupBy(x => new { x.InvoiceDate.Year, x.InvoiceDate.Month })
            .Select(g => new MonthlyPurchaseDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month) + " " + g.Key.Year,
                BillCount = g.Count(),
                GrossPurchase = g.Sum(x => x.SubTotal),
                TaxAmount = g.Sum(x => x.TaxAmount),
                NetPurchase = g.Sum(x => x.GrandTotal),
                PaidAmount = g.Sum(x => x.PaidAmount)
            })
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .ToListAsync(ct);
    }

    public async Task<List<PurchaseRegisterDto>> GetPurchaseRegisterAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBasePurchaseQuery(filter);

        return await query
            .Include(x => x.Supplier)
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new PurchaseRegisterDto
            {
                InvoiceId = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                InvoiceDate = x.InvoiceDate,
                SupplierName = x.Supplier != null ? x.Supplier.SupplierName : "Supplier",
                GSTNumber = x.Supplier != null ? x.Supplier.GSTNumber : null,
                ReferenceNumber = x.ReferenceNumber,
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

    public async Task<List<SupplierWisePurchaseDto>> GetSupplierWisePurchaseAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBasePurchaseQuery(filter);

        return await query
            .GroupBy(x => new { x.SupplierId, SupplierName = x.Supplier != null ? x.Supplier.SupplierName : "Supplier", SupplierCode = x.Supplier != null ? x.Supplier.SupplierCode : "", Mobile = x.Supplier != null ? x.Supplier.Mobile : "", City = x.Supplier != null ? x.Supplier.City : "" })
            .Select(g => new SupplierWisePurchaseDto
            {
                SupplierId = g.Key.SupplierId,
                SupplierCode = g.Key.SupplierCode,
                SupplierName = g.Key.SupplierName,
                Mobile = g.Key.Mobile,
                City = g.Key.City,
                BillCount = g.Count(),
                TotalPurchaseAmount = g.Sum(x => x.GrandTotal),
                PaidAmount = g.Sum(x => x.PaidAmount),
                BalanceAmount = g.Sum(x => x.BalanceAmount)
            })
            .OrderByDescending(x => x.TotalPurchaseAmount)
            .ToListAsync(ct);
    }

    public async Task<List<ProductWisePurchaseDto>> GetProductWisePurchaseAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);

        var query = _context.PurchaseInvoiceItems.AsNoTracking()
            .Where(i => i.PurchaseInvoice != null && i.PurchaseInvoice.IsActive && i.PurchaseInvoice.Status != "Cancelled" &&
                        i.PurchaseInvoice.InvoiceDate >= fromDate && i.PurchaseInvoice.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.PurchaseInvoice!.CompanyId == ActiveCompanyId.Value);

        if (filter.SupplierId.HasValue && filter.SupplierId.Value > 0)
            query = query.Where(i => i.PurchaseInvoice!.SupplierId == filter.SupplierId.Value);

        if (filter.ProductId.HasValue && filter.ProductId.Value > 0)
            query = query.Where(i => i.ProductId == filter.ProductId.Value);

        return await query
            .GroupBy(i => new { i.ProductId, i.ProductName, i.UnitName, CategoryName = i.Product != null && i.Product.Category != null ? i.Product.Category.CategoryName : "General", ProductCode = i.Product != null ? i.Product.ProductCode : "" })
            .Select(g => new ProductWisePurchaseDto
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                CategoryName = g.Key.CategoryName,
                UnitName = g.Key.UnitName ?? "Nos",
                QuantityPurchased = g.Sum(x => x.Quantity),
                TotalPurchaseAmount = g.Sum(x => x.Amount),
                AverageRate = g.Sum(x => x.Quantity) > 0 ? Math.Round(g.Sum(x => x.Amount) / g.Sum(x => x.Quantity), 2) : 0,
                TotalTaxAmount = g.Sum(x => x.TaxAmount)
            })
            .OrderByDescending(x => x.TotalPurchaseAmount)
            .ToListAsync(ct);
    }

    public async Task<List<PurchaseReturnReportDto>> GetPurchaseReturnReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.PurchaseReturns.AsNoTracking()
            .Include(r => r.Supplier)
            .Include(r => r.PurchaseInvoice)
            .Where(r => r.IsActive && r.ReturnDate >= fromDate && r.ReturnDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(r => r.CompanyId == ActiveCompanyId.Value);

        if (filter.SupplierId.HasValue && filter.SupplierId.Value > 0)
            query = query.Where(r => r.SupplierId == filter.SupplierId.Value);

        return await query
            .OrderByDescending(r => r.ReturnDate)
            .Select(r => new PurchaseReturnReportDto
            {
                ReturnId = r.Id,
                ReturnNumber = r.ReturnNumber,
                ReturnDate = r.ReturnDate,
                SupplierName = r.Supplier != null ? r.Supplier.SupplierName : "Supplier",
                OriginalInvoiceNumber = r.PurchaseInvoice != null ? r.PurchaseInvoice.InvoiceNumber : null,
                Reason = r.Reason,
                SubTotal = r.SubTotal,
                TaxAmount = r.TaxAmount,
                GrandTotal = r.GrandTotal,
                Status = r.Status
            })
            .ToListAsync(ct);
    }

    public async Task<List<PurchaseOrderReportDto>> GetPurchaseOrderReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.PurchaseOrders.AsNoTracking()
            .Include(o => o.Supplier)
            .Where(o => o.IsActive && o.OrderDate >= fromDate && o.OrderDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(o => o.CompanyId == ActiveCompanyId.Value);

        if (filter.SupplierId.HasValue && filter.SupplierId.Value > 0)
            query = query.Where(o => o.SupplierId == filter.SupplierId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(o => o.Status == filter.Status);

        return await query
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new PurchaseOrderReportDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                ExpectedDeliveryDate = o.DeliveryDate,
                SupplierName = o.Supplier != null ? o.Supplier.SupplierName : "Supplier",
                GrandTotal = o.GrandTotal,
                Status = o.Status,
                ReferenceNumber = o.ReferenceNumber
            })
            .ToListAsync(ct);
    }

    public async Task<List<PurchaseOrderReportDto>> GetPendingPurchaseOrdersAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        filter.Status = "Pending";
        var query = _context.PurchaseOrders.AsNoTracking()
            .Include(o => o.Supplier)
            .Where(o => o.IsActive && (o.Status == "Pending" || o.Status == "Draft" || o.Status == "Confirmed"));

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(o => o.CompanyId == ActiveCompanyId.Value);

        if (filter.SupplierId.HasValue && filter.SupplierId.Value > 0)
            query = query.Where(o => o.SupplierId == filter.SupplierId.Value);

        return await query
            .OrderBy(o => o.DeliveryDate ?? o.OrderDate)
            .Select(o => new PurchaseOrderReportDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                ExpectedDeliveryDate = o.DeliveryDate,
                SupplierName = o.Supplier != null ? o.Supplier.SupplierName : "Supplier",
                GrandTotal = o.GrandTotal,
                Status = o.Status,
                ReferenceNumber = o.ReferenceNumber
            })
            .ToListAsync(ct);
    }

    public async Task<List<GoodsReceiptReportDto>> GetGoodsReceiptReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.GoodsReceiptNotes.AsNoTracking()
            .Include(g => g.Supplier)
            .Where(g => g.IsActive && g.GRNDate >= fromDate && g.GRNDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(g => g.CompanyId == ActiveCompanyId.Value);

        if (filter.SupplierId.HasValue && filter.SupplierId.Value > 0)
            query = query.Where(g => g.SupplierId == filter.SupplierId.Value);

        return await query
            .OrderByDescending(g => g.GRNDate)
            .Select(g => new GoodsReceiptReportDto
            {
                GrnId = g.Id,
                GrnNumber = g.GRNNumber,
                GrnDate = g.GRNDate,
                SupplierName = g.Supplier != null ? g.Supplier.SupplierName : "Supplier",
                PurchaseOrderNumber = g.PurchaseOrderId.HasValue ? ("PO-" + g.PurchaseOrderId.Value) : null,
                ChallanNumber = g.ChalanNumber,
                GrandTotal = g.GrandTotal,
                Status = g.Status
            })
            .ToListAsync(ct);
    }

    public async Task<List<PurchasePaymentReportDto>> GetPurchasePaymentReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBasePurchaseQuery(filter);

        return await query
            .Include(x => x.Supplier)
            .OrderByDescending(x => x.InvoiceDate)
            .Select(x => new PurchasePaymentReportDto
            {
                InvoiceId = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                InvoiceDate = x.InvoiceDate,
                SupplierName = x.Supplier != null ? x.Supplier.SupplierName : "Supplier",
                GrandTotal = x.GrandTotal,
                PaidAmount = x.PaidAmount,
                BalanceAmount = x.BalanceAmount
            })
            .ToListAsync(ct);
    }

    public async Task<List<PurchasePriceHistoryDto>> GetPurchasePriceHistoryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.PurchaseInvoiceItems.AsNoTracking()
            .Include(i => i.PurchaseInvoice)
                .ThenInclude(p => p!.Supplier)
            .Where(i => i.PurchaseInvoice != null && i.PurchaseInvoice.IsActive && i.PurchaseInvoice.Status != "Cancelled" &&
                        i.PurchaseInvoice.InvoiceDate >= fromDate && i.PurchaseInvoice.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.PurchaseInvoice!.CompanyId == ActiveCompanyId.Value);

        if (filter.ProductId.HasValue && filter.ProductId.Value > 0)
            query = query.Where(i => i.ProductId == filter.ProductId.Value);

        return await query
            .OrderByDescending(i => i.PurchaseInvoice!.InvoiceDate)
            .Take(500)
            .Select(i => new PurchasePriceHistoryDto
            {
                ProductId = i.ProductId,
                ProductCode = i.Product != null ? i.Product.ProductCode : "",
                ProductName = i.ProductName,
                InvoiceDate = i.PurchaseInvoice!.InvoiceDate,
                SupplierName = i.PurchaseInvoice.Supplier != null ? i.PurchaseInvoice.Supplier.SupplierName : "Supplier",
                InvoiceNumber = i.PurchaseInvoice.InvoiceNumber,
                Quantity = i.Quantity,
                PurchaseRate = i.Rate
            })
            .ToListAsync(ct);
    }

    public async Task<List<PurchaseTaxSummaryDto>> GetPurchaseTaxSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.PurchaseInvoiceItems.AsNoTracking()
            .Where(i => i.PurchaseInvoice != null && i.PurchaseInvoice.IsActive && i.PurchaseInvoice.Status != "Cancelled" &&
                        i.PurchaseInvoice.InvoiceDate >= fromDate && i.PurchaseInvoice.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.PurchaseInvoice!.CompanyId == ActiveCompanyId.Value);

        return await query
            .GroupBy(i => i.TaxPercentage)
            .Select(g => new PurchaseTaxSummaryDto
            {
                TaxPercentage = g.Key,
                TaxableAmount = g.Sum(x => x.Amount - x.TaxAmount),
                CGSTAmount = g.Sum(x => x.CGSTAmount),
                SGSTAmount = g.Sum(x => x.SGSTAmount),
                IGSTAmount = g.Sum(x => x.IGSTAmount)
            })
            .OrderBy(x => x.TaxPercentage)
            .ToListAsync(ct);
    }

    public async Task<List<ReportSummaryCard>> GetPurchaseSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBasePurchaseQuery(filter);

        var totalBills = await query.CountAsync(ct);
        var grossPurchase = await query.SumAsync(x => (decimal?)x.SubTotal, ct) ?? 0m;
        var totalTax = await query.SumAsync(x => (decimal?)x.TaxAmount, ct) ?? 0m;
        var totalGrand = await query.SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0m;
        var totalPaid = await query.SumAsync(x => (decimal?)x.PaidAmount, ct) ?? 0m;
        var totalBalance = await query.SumAsync(x => (decimal?)x.BalanceAmount, ct) ?? 0m;

        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var returnQuery = _context.PurchaseReturns.AsNoTracking()
            .Where(r => r.IsActive && r.Status != "Cancelled" && r.ReturnDate >= fromDate && r.ReturnDate <= toDate);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            returnQuery = returnQuery.Where(r => r.CompanyId == ActiveCompanyId.Value);

        var totalReturns = await returnQuery.SumAsync(r => (decimal?)r.GrandTotal, ct) ?? 0m;
        var netPurchase = totalGrand - totalReturns;

        return new List<ReportSummaryCard>
        {
            new() { Title = "Total Purchase Bills", Value = totalBills.ToString("N0"), Icon = "fa-bag-shopping", ColorClass = "primary" },
            new() { Title = "Gross Purchases", Value = $"₹{grossPurchase:N2}", Icon = "fa-receipt", ColorClass = "info" },
            new() { Title = "Input Tax Credit (ITC)", Value = $"₹{totalTax:N2}", Icon = "fa-shield-halved", ColorClass = "warning" },
            new() { Title = "Net Purchase", Value = $"₹{netPurchase:N2}", Icon = "fa-indian-rupee-sign", ColorClass = "success" },
            new() { Title = "Amount Paid", Value = $"₹{totalPaid:N2}", Icon = "fa-credit-card", ColorClass = "success" },
            new() { Title = "Outstanding Payable", Value = $"₹{totalBalance:N2}", Icon = "fa-clock", ColorClass = "danger" }
        };
    }
}
