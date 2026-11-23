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

public class GstReportService : IGstReportService
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;

    public GstReportService(AppDbContext context, ICompanyContext companyContext)
    {
        _context = context;
        _companyContext = companyContext;
    }

    private int? ActiveCompanyId => _companyContext.CurrentCompanyId;

    public async Task<List<GstSalesSummaryDto>> GetGstSalesSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.SalesInvoices.AsNoTracking()
            .Include(s => s.Customer)
            .Where(s => s.IsActive && s.Status != "Cancelled" && s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(s => s.CompanyId == ActiveCompanyId.Value);

        var invoices = await query.ToListAsync(ct);

        // Group into B2B (with GSTIN) and B2C (without GSTIN)
        var b2b = invoices.Where(x => !string.IsNullOrWhiteSpace(x.Customer?.GSTNumber)).ToList();
        var b2c = invoices.Where(x => string.IsNullOrWhiteSpace(x.Customer?.GSTNumber)).ToList();

        var result = new List<GstSalesSummaryDto>();

        if (b2b.Any())
        {
            result.Add(new GstSalesSummaryDto
            {
                SupplyType = "B2B (Registered Customers)",
                InvoiceCount = b2b.Count,
                TaxableValue = b2b.Sum(x => x.SubTotal),
                CGSTAmount = b2b.Sum(x => x.Items.Sum(i => i.CGSTAmount)),
                SGSTAmount = b2b.Sum(x => x.Items.Sum(i => i.SGSTAmount)),
                IGSTAmount = b2b.Sum(x => x.Items.Sum(i => i.IGSTAmount))
            });
        }

        if (b2c.Any())
        {
            result.Add(new GstSalesSummaryDto
            {
                SupplyType = "B2C (Consumers / Unregistered)",
                InvoiceCount = b2c.Count,
                TaxableValue = b2c.Sum(x => x.SubTotal),
                CGSTAmount = b2c.Sum(x => x.Items.Sum(i => i.CGSTAmount)),
                SGSTAmount = b2c.Sum(x => x.Items.Sum(i => i.SGSTAmount)),
                IGSTAmount = b2c.Sum(x => x.Items.Sum(i => i.IGSTAmount))
            });
        }

        return result;
    }

    public async Task<List<GstPurchaseSummaryDto>> GetGstPurchaseSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.PurchaseInvoices.AsNoTracking()
            .Include(p => p.Supplier)
            .Where(p => p.IsActive && p.Status != "Cancelled" && p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(p => p.CompanyId == ActiveCompanyId.Value);

        var bills = await query.ToListAsync(ct);

        var b2b = bills.Where(x => !string.IsNullOrWhiteSpace(x.Supplier?.GSTNumber)).ToList();
        var b2c = bills.Where(x => string.IsNullOrWhiteSpace(x.Supplier?.GSTNumber)).ToList();

        var result = new List<GstPurchaseSummaryDto>();

        if (b2b.Any())
        {
            result.Add(new GstPurchaseSummaryDto
            {
                SupplyType = "Inward Supplies from Registered Persons (Eligible ITC)",
                BillCount = b2b.Count,
                TaxableValue = b2b.Sum(x => x.SubTotal),
                EligibleCGST = b2b.Sum(x => x.Items.Sum(i => i.CGSTAmount)),
                EligibleSGST = b2b.Sum(x => x.Items.Sum(i => i.SGSTAmount)),
                EligibleIGST = b2b.Sum(x => x.Items.Sum(i => i.IGSTAmount))
            });
        }

        if (b2c.Any())
        {
            result.Add(new GstPurchaseSummaryDto
            {
                SupplyType = "Inward Supplies from Unregistered Persons",
                BillCount = b2c.Count,
                TaxableValue = b2c.Sum(x => x.SubTotal),
                EligibleCGST = 0m,
                EligibleSGST = 0m,
                EligibleIGST = 0m
            });
        }

        return result;
    }

    public async Task<List<HsnSummaryDto>> GetHsnWiseSalesAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= fromDate && i.SalesInvoice.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.SalesInvoice!.CompanyId == ActiveCompanyId.Value);

        return await query
            .GroupBy(i => new { HSN = string.IsNullOrWhiteSpace(i.HSNCode) ? "OTHER" : i.HSNCode, i.ProductName, Unit = i.UnitName ?? "Nos", i.TaxPercentage })
            .Select(g => new HsnSummaryDto
            {
                HSNCode = g.Key.HSN,
                Description = g.Key.ProductName,
                UnitName = g.Key.Unit,
                RateOfTax = g.Key.TaxPercentage,
                TotalQuantity = g.Sum(x => x.Quantity),
                TaxableValue = g.Sum(x => x.Amount - x.TaxAmount),
                CGSTAmount = g.Sum(x => x.CGSTAmount),
                SGSTAmount = g.Sum(x => x.SGSTAmount),
                IGSTAmount = g.Sum(x => x.IGSTAmount)
            })
            .OrderBy(x => x.HSNCode)
            .ToListAsync(ct);
    }

    public async Task<List<HsnSummaryDto>> GetHsnWisePurchaseAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.PurchaseInvoiceItems.AsNoTracking()
            .Where(i => i.PurchaseInvoice != null && i.PurchaseInvoice.IsActive && i.PurchaseInvoice.Status != "Cancelled" &&
                        i.PurchaseInvoice.InvoiceDate >= fromDate && i.PurchaseInvoice.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.PurchaseInvoice!.CompanyId == ActiveCompanyId.Value);

        return await query
            .GroupBy(i => new { HSN = string.IsNullOrWhiteSpace(i.HSNCode) ? "OTHER" : i.HSNCode, i.ProductName, Unit = i.UnitName ?? "Nos", i.TaxPercentage })
            .Select(g => new HsnSummaryDto
            {
                HSNCode = g.Key.HSN,
                Description = g.Key.ProductName,
                UnitName = g.Key.Unit,
                RateOfTax = g.Key.TaxPercentage,
                TotalQuantity = g.Sum(x => x.Quantity),
                TaxableValue = g.Sum(x => x.Amount - x.TaxAmount),
                CGSTAmount = g.Sum(x => x.CGSTAmount),
                SGSTAmount = g.Sum(x => x.SGSTAmount),
                IGSTAmount = g.Sum(x => x.IGSTAmount)
            })
            .OrderBy(x => x.HSNCode)
            .ToListAsync(ct);
    }

    public async Task<List<TaxBreakdownDto>> GetCgstSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= fromDate && i.SalesInvoice.InvoiceDate <= toDate && i.CGSTAmount > 0);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.SalesInvoice!.CompanyId == ActiveCompanyId.Value);

        return await query
            .GroupBy(i => i.TaxPercentage / 2m) // CGST rate is half of GST rate
            .Select(g => new TaxBreakdownDto
            {
                TaxType = "Central GST (CGST)",
                TaxRate = g.Key,
                TaxableAmount = g.Sum(x => x.Amount - x.TaxAmount),
                TaxAmount = g.Sum(x => x.CGSTAmount)
            })
            .OrderBy(x => x.TaxRate)
            .ToListAsync(ct);
    }

    public async Task<List<TaxBreakdownDto>> GetSgstSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= fromDate && i.SalesInvoice.InvoiceDate <= toDate && i.SGSTAmount > 0);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.SalesInvoice!.CompanyId == ActiveCompanyId.Value);

        return await query
            .GroupBy(i => i.TaxPercentage / 2m) // SGST rate is half of GST rate
            .Select(g => new TaxBreakdownDto
            {
                TaxType = "State GST (SGST)",
                TaxRate = g.Key,
                TaxableAmount = g.Sum(x => x.Amount - x.TaxAmount),
                TaxAmount = g.Sum(x => x.SGSTAmount)
            })
            .OrderBy(x => x.TaxRate)
            .ToListAsync(ct);
    }

    public async Task<List<TaxBreakdownDto>> GetIgstSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= fromDate && i.SalesInvoice.InvoiceDate <= toDate && i.IGSTAmount > 0);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.SalesInvoice!.CompanyId == ActiveCompanyId.Value);

        return await query
            .GroupBy(i => i.TaxPercentage)
            .Select(g => new TaxBreakdownDto
            {
                TaxType = "Integrated GST (IGST)",
                TaxRate = g.Key,
                TaxableAmount = g.Sum(x => x.Amount - x.TaxAmount),
                TaxAmount = g.Sum(x => x.IGSTAmount)
            })
            .OrderBy(x => x.TaxRate)
            .ToListAsync(ct);
    }

    public async Task<List<TaxRateSummaryDto>> GetTaxRateWiseSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= fromDate && i.SalesInvoice.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.SalesInvoice!.CompanyId == ActiveCompanyId.Value);

        return await query
            .GroupBy(i => i.TaxPercentage)
            .Select(g => new TaxRateSummaryDto
            {
                TaxRate = g.Key,
                TaxableValue = g.Sum(x => x.Amount - x.TaxAmount),
                CGSTAmount = g.Sum(x => x.CGSTAmount),
                SGSTAmount = g.Sum(x => x.SGSTAmount),
                IGSTAmount = g.Sum(x => x.IGSTAmount)
            })
            .OrderBy(x => x.TaxRate)
            .ToListAsync(ct);
    }

    public async Task<List<SalesReturnReportDto>> GetSalesReturnTaxSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.SalesReturns.AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.SalesInvoice)
            .Where(r => r.IsActive && r.ReturnDate >= fromDate && r.ReturnDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(r => r.CompanyId == ActiveCompanyId.Value);

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

    public async Task<List<PurchaseReturnReportDto>> GetPurchaseReturnTaxSummaryAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.PurchaseReturns.AsNoTracking()
            .Include(r => r.Supplier)
            .Include(r => r.PurchaseInvoice)
            .Where(r => r.IsActive && r.ReturnDate >= fromDate && r.ReturnDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(r => r.CompanyId == ActiveCompanyId.Value);

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

    public async Task<List<ReportSummaryCard>> GetGstSummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var salesRates = await GetTaxRateWiseSummaryAsync(filter, ct);
        var totalOutputTax = salesRates.Sum(x => x.TotalTax);
        var totalTaxableSales = salesRates.Sum(x => x.TaxableValue);

        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var purchaseQuery = _context.PurchaseInvoiceItems.AsNoTracking()
            .Where(i => i.PurchaseInvoice != null && i.PurchaseInvoice.IsActive && i.PurchaseInvoice.Status != "Cancelled" &&
                        i.PurchaseInvoice.InvoiceDate >= fromDate && i.PurchaseInvoice.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            purchaseQuery = purchaseQuery.Where(i => i.PurchaseInvoice!.CompanyId == ActiveCompanyId.Value);

        var totalInputTax = await purchaseQuery.SumAsync(i => (decimal?)i.TaxAmount, ct) ?? 0m;
        var netTaxPayable = totalOutputTax - totalInputTax;

        return new List<ReportSummaryCard>
        {
            new() { Title = "Taxable Sales Value", Value = $"₹{totalTaxableSales:N2}", Icon = "fa-cart-shopping", ColorClass = "primary" },
            new() { Title = "Output GST (Sales)", Value = $"₹{totalOutputTax:N2}", Icon = "fa-arrow-up-right-from-square", ColorClass = "danger" },
            new() { Title = "Input GST Credit (Purchase)", Value = $"₹{totalInputTax:N2}", Icon = "fa-shield-halved", ColorClass = "success" },
            new() { Title = "Net GST Liability", Value = $"₹{netTaxPayable:N2}", Icon = "fa-scale-balanced", ColorClass = netTaxPayable >= 0 ? "warning" : "info" }
        };
    }
}
