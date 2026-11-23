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

public class InventoryReportService : IInventoryReportService
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;

    public InventoryReportService(AppDbContext context, ICompanyContext companyContext)
    {
        _context = context;
        _companyContext = companyContext;
    }

    private int? ActiveCompanyId => _companyContext.CurrentCompanyId;

    private IQueryable<ERP.Models.Product> GetBaseProductQuery(ReportFilterViewModel filter)
    {
        var query = _context.Products.AsNoTracking().Where(p => p.IsActive);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(p => p.CompanyId == ActiveCompanyId.Value);

        if (filter.ProductId.HasValue && filter.ProductId.Value > 0)
            query = query.Where(p => p.Id == filter.ProductId.Value);

        if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        if (filter.BrandId.HasValue && filter.BrandId.Value > 0)
            query = query.Where(p => p.BrandId == filter.BrandId.Value);

        if (filter.WarehouseId.HasValue && filter.WarehouseId.Value > 0)
            query = query.Where(p => p.WarehouseId == filter.WarehouseId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(p => p.ProductName.ToLower().Contains(search) || p.ProductCode.ToLower().Contains(search) || (p.Barcode != null && p.Barcode.ToLower().Contains(search)));
        }

        return query;
    }

    public async Task<List<CurrentStockDto>> GetCurrentStockReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseProductQuery(filter);

        return await query
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.Warehouse)
            .OrderBy(p => p.ProductName)
            .Select(p => new CurrentStockDto
            {
                ProductId = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                CategoryName = p.Category != null ? p.Category.CategoryName : "Uncategorized",
                BrandName = p.Brand != null ? p.Brand.BrandName : "Unbranded",
                WarehouseName = p.Warehouse != null ? p.Warehouse.WarehouseName : "Main Store",
                UnitName = p.Unit != null ? p.Unit.UnitName : "Nos",
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock,
                ReorderLevel = p.ReorderLevel,
                PurchasePrice = p.PurchasePrice,
                SalesPrice = p.SalesPrice
            })
            .ToListAsync(ct);
    }

    public async Task<List<CurrentStockDto>> GetLowStockReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseProductQuery(filter)
            .Where(p => p.CurrentStock > 0 && (p.CurrentStock <= p.MinimumStock || p.CurrentStock <= p.ReorderLevel));

        return await query
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.Warehouse)
            .OrderBy(p => p.CurrentStock)
            .Select(p => new CurrentStockDto
            {
                ProductId = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                CategoryName = p.Category != null ? p.Category.CategoryName : "Uncategorized",
                BrandName = p.Brand != null ? p.Brand.BrandName : "Unbranded",
                WarehouseName = p.Warehouse != null ? p.Warehouse.WarehouseName : "Main Store",
                UnitName = p.Unit != null ? p.Unit.UnitName : "Nos",
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock,
                ReorderLevel = p.ReorderLevel,
                PurchasePrice = p.PurchasePrice,
                SalesPrice = p.SalesPrice
            })
            .ToListAsync(ct);
    }

    public async Task<List<CurrentStockDto>> GetOutOfStockReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseProductQuery(filter).Where(p => p.CurrentStock <= 0);

        return await query
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.Warehouse)
            .OrderBy(p => p.ProductName)
            .Select(p => new CurrentStockDto
            {
                ProductId = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                CategoryName = p.Category != null ? p.Category.CategoryName : "Uncategorized",
                BrandName = p.Brand != null ? p.Brand.BrandName : "Unbranded",
                WarehouseName = p.Warehouse != null ? p.Warehouse.WarehouseName : "Main Store",
                UnitName = p.Unit != null ? p.Unit.UnitName : "Nos",
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock,
                ReorderLevel = p.ReorderLevel,
                PurchasePrice = p.PurchasePrice,
                SalesPrice = p.SalesPrice
            })
            .ToListAsync(ct);
    }

    public async Task<List<StockLedgerRowDto>> GetStockLedgerAsync(int productId, int? warehouseId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (product == null) return new List<StockLedgerRowDto>();

        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var txQuery = _context.StockTransactions.AsNoTracking()
            .Include(t => t.Warehouse)
            .Where(t => t.ProductId == productId && t.IsActive);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            txQuery = txQuery.Where(t => t.CompanyId == ActiveCompanyId.Value);

        if (warehouseId.HasValue && warehouseId.Value > 0)
            txQuery = txQuery.Where(t => t.WarehouseId == warehouseId.Value);

        // Historical Opening Balance up to fromDate:
        // product.OpeningStock + sum of all transactions before fromDate
        var priorTxSum = await txQuery
            .Where(t => t.TransactionDate < from)
            .SumAsync(t => (decimal?)t.Quantity, ct) ?? 0m;

        decimal initialBalance = product.OpeningStock + priorTxSum;

        var periodTxs = await txQuery
            .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.Id)
            .ToListAsync(ct);

        var result = new List<StockLedgerRowDto>();

        // Add Initial Balance row
        result.Add(new StockLedgerRowDto
        {
            TransactionDate = from,
            TransactionType = "Opening Balance",
            ReferenceNumber = "OPENING",
            WarehouseName = product.WarehouseId.HasValue ? "Assigned Warehouse" : "Main",
            QuantityIn = initialBalance >= 0 ? initialBalance : 0,
            QuantityOut = initialBalance < 0 ? Math.Abs(initialBalance) : 0,
            BalanceQuantity = initialBalance,
            Rate = product.PurchasePrice,
            Remarks = "Calculated Opening Stock as of period start"
        });

        decimal runningBalance = initialBalance;
        foreach (var tx in periodTxs)
        {
            var qtyIn = tx.Quantity > 0 ? tx.Quantity : 0;
            var qtyOut = tx.Quantity < 0 ? Math.Abs(tx.Quantity) : 0;
            runningBalance += tx.Quantity;

            result.Add(new StockLedgerRowDto
            {
                TransactionDate = tx.TransactionDate,
                TransactionType = tx.TransactionType,
                ReferenceNumber = tx.ReferenceNumber,
                WarehouseName = tx.Warehouse?.WarehouseName ?? "Main",
                QuantityIn = qtyIn,
                QuantityOut = qtyOut,
                BalanceQuantity = runningBalance,
                Rate = tx.Rate,
                Remarks = tx.Remarks
            });
        }

        return result;
    }

    public async Task<List<StockMovementDto>> GetStockMovementReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var products = await GetBaseProductQuery(filter).Include(p => p.Category).Include(p => p.Unit).ToListAsync(ct);

        var txQuery = _context.StockTransactions.AsNoTracking().Where(t => t.IsActive);
        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            txQuery = txQuery.Where(t => t.CompanyId == ActiveCompanyId.Value);

        // Pre-period sum by product
        var priorTxs = await txQuery
            .Where(t => t.TransactionDate < fromDate)
            .GroupBy(t => t.ProductId)
            .Select(g => new { ProductId = g.Key, PriorSum = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.PriorSum, ct);

        // Period movements by product
        var periodTxs = await txQuery
            .Where(t => t.TransactionDate >= fromDate && t.TransactionDate <= toDate)
            .GroupBy(t => t.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Inward = g.Where(x => x.Quantity > 0).Sum(x => x.Quantity),
                Outward = g.Where(x => x.Quantity < 0).Sum(x => Math.Abs(x.Quantity))
            })
            .ToDictionaryAsync(x => x.ProductId, x => x, ct);

        var result = new List<StockMovementDto>();
        foreach (var p in products)
        {
            priorTxs.TryGetValue(p.Id, out var priorQty);
            decimal opening = p.OpeningStock + priorQty;

            decimal inward = 0m;
            decimal outward = 0m;
            if (periodTxs.TryGetValue(p.Id, out var periodMovement))
            {
                inward = periodMovement.Inward;
                outward = periodMovement.Outward;
            }

            result.Add(new StockMovementDto
            {
                ProductId = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                CategoryName = p.Category?.CategoryName ?? "General",
                UnitName = p.Unit?.UnitName ?? "Nos",
                OpeningStock = opening,
                InwardQuantity = inward,
                OutwardQuantity = outward
            });
        }

        return result.OrderBy(x => x.ProductName).ToList();
    }

    public async Task<List<CurrentStockDto>> GetStockValuationReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        return await GetCurrentStockReportAsync(filter, ct);
    }

    public async Task<List<CurrentStockDto>> GetOpeningStockReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var products = await GetBaseProductQuery(filter)
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.Warehouse)
            .Where(p => p.OpeningStock != 0)
            .OrderBy(p => p.ProductName)
            .Select(p => new CurrentStockDto
            {
                ProductId = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                CategoryName = p.Category != null ? p.Category.CategoryName : "Uncategorized",
                WarehouseName = p.Warehouse != null ? p.Warehouse.WarehouseName : "Main Store",
                UnitName = p.Unit != null ? p.Unit.UnitName : "Nos",
                CurrentStock = p.OpeningStock,
                PurchasePrice = p.PurchasePrice,
                SalesPrice = p.SalesPrice
            })
            .ToListAsync(ct);

        return products;
    }

    public async Task<List<CurrentStockDto>> GetClosingStockReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var products = await GetBaseProductQuery(filter).Include(p => p.Category).Include(p => p.Unit).Include(p => p.Warehouse).ToListAsync(ct);

        // For historical closing stock as of toDate:
        // Calculate by undoing subsequent transactions: ClosingStockAtToDate = CurrentStock - Sum(tx.Quantity where tx.TransactionDate > toDate)
        var futureTxs = await _context.StockTransactions.AsNoTracking()
            .Where(t => t.IsActive && t.TransactionDate > toDate && (ActiveCompanyId == null || t.CompanyId == ActiveCompanyId))
            .GroupBy(t => t.ProductId)
            .Select(g => new { ProductId = g.Key, FutureSum = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.FutureSum, ct);

        var result = new List<CurrentStockDto>();
        foreach (var p in products)
        {
            futureTxs.TryGetValue(p.Id, out var futureQty);
            decimal historicalStock = p.CurrentStock - futureQty;

            result.Add(new CurrentStockDto
            {
                ProductId = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                CategoryName = p.Category?.CategoryName ?? "General",
                WarehouseName = p.Warehouse?.WarehouseName ?? "Main Store",
                UnitName = p.Unit?.UnitName ?? "Nos",
                CurrentStock = historicalStock,
                MinimumStock = p.MinimumStock,
                ReorderLevel = p.ReorderLevel,
                PurchasePrice = p.PurchasePrice,
                SalesPrice = p.SalesPrice
            });
        }

        return result.OrderBy(x => x.ProductName).ToList();
    }

    public async Task<List<StockTransferReportDto>> GetStockTransferReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.StockTransfers.AsNoTracking()
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Items)
            .Where(t => t.IsActive && t.TransferDate >= fromDate && t.TransferDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(t => t.CompanyId == ActiveCompanyId.Value);

        return await query
            .OrderByDescending(t => t.TransferDate)
            .Select(t => new StockTransferReportDto
            {
                TransferId = t.Id,
                TransferNumber = t.TransferNumber,
                TransferDate = t.TransferDate,
                FromWarehouse = t.FromWarehouse != null ? t.FromWarehouse.WarehouseName : "Warehouse A",
                ToWarehouse = t.ToWarehouse != null ? t.ToWarehouse.WarehouseName : "Warehouse B",
                ItemCount = t.Items.Count,
                TotalQuantity = t.Items.Sum(i => i.Quantity),
                Status = t.Status,
                Remarks = t.Remarks
            })
            .ToListAsync(ct);
    }

    public async Task<List<StockAdjustmentReportDto>> GetStockAdjustmentReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.StockAdjustments.AsNoTracking()
            .Include(a => a.Warehouse)
            .Include(a => a.Items)
            .Where(a => a.IsActive && a.AdjustmentDate >= fromDate && a.AdjustmentDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(a => a.CompanyId == ActiveCompanyId.Value);

        return await query
            .OrderByDescending(a => a.AdjustmentDate)
            .Select(a => new StockAdjustmentReportDto
            {
                AdjustmentId = a.Id,
                AdjustmentNumber = a.AdjustmentNumber,
                AdjustmentDate = a.AdjustmentDate,
                WarehouseName = a.Warehouse != null ? a.Warehouse.WarehouseName : "Main Store",
                AdjustmentType = a.AdjustmentType,
                ItemCount = a.Items.Count,
                TotalQuantity = a.Items.Sum(i => i.Quantity),
                Remarks = a.Remarks
            })
            .ToListAsync(ct);
    }

    public async Task<List<PhysicalVerificationReportDto>> GetPhysicalStockVerificationReportAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var query = _context.PhysicalStockVerifications.AsNoTracking()
            .Include(v => v.Warehouse)
            .Include(v => v.Items)
            .Where(v => v.IsActive && v.VerificationDate >= fromDate && v.VerificationDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(v => v.CompanyId == ActiveCompanyId.Value);

        return await query
            .OrderByDescending(v => v.VerificationDate)
            .Select(v => new PhysicalVerificationReportDto
            {
                VerificationId = v.Id,
                VerificationNumber = v.VerificationNumber,
                VerificationDate = v.VerificationDate,
                WarehouseName = v.Warehouse != null ? v.Warehouse.WarehouseName : "Main Store",
                ItemCount = v.Items.Count,
                TotalVariance = v.Items.Sum(i => i.Variance),
                Remarks = v.Remarks
            })
            .ToListAsync(ct);
    }

    public async Task<List<ProductMovementAnalysisDto>> GetFastMovingProductsAsync(ReportFilterViewModel filter, int limit = 20, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);

        var query = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= fromDate && i.SalesInvoice.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.SalesInvoice!.CompanyId == ActiveCompanyId.Value);

        var grouped = await query
            .GroupBy(i => new { i.ProductId, i.ProductName, CategoryName = i.Product != null && i.Product.Category != null ? i.Product.Category.CategoryName : "General", ProductCode = i.Product != null ? i.Product.ProductCode : "", CurrentStock = i.Product != null ? i.Product.CurrentStock : 0 })
            .Select(g => new ProductMovementAnalysisDto
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                CategoryName = g.Key.CategoryName,
                CurrentStock = g.Key.CurrentStock,
                QuantitySold = g.Sum(x => x.Quantity),
                TotalSalesAmount = g.Sum(x => x.Amount),
                TransactionCount = g.Count(),
                MovementClassification = "Fast Moving"
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(limit)
            .ToListAsync(ct);

        return grouped;
    }

    public async Task<List<ProductMovementAnalysisDto>> GetSlowMovingProductsAsync(ReportFilterViewModel filter, int limit = 20, CancellationToken ct = default)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);

        var query = _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= fromDate && i.SalesInvoice.InvoiceDate <= toDate);

        if (ActiveCompanyId.HasValue && ActiveCompanyId.Value > 0)
            query = query.Where(i => i.SalesInvoice!.CompanyId == ActiveCompanyId.Value);

        var grouped = await query
            .GroupBy(i => new { i.ProductId, i.ProductName, CategoryName = i.Product != null && i.Product.Category != null ? i.Product.Category.CategoryName : "General", ProductCode = i.Product != null ? i.Product.ProductCode : "", CurrentStock = i.Product != null ? i.Product.CurrentStock : 0 })
            .Select(g => new ProductMovementAnalysisDto
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                CategoryName = g.Key.CategoryName,
                CurrentStock = g.Key.CurrentStock,
                QuantitySold = g.Sum(x => x.Quantity),
                TotalSalesAmount = g.Sum(x => x.Amount),
                TransactionCount = g.Count(),
                MovementClassification = "Slow Moving"
            })
            .Where(x => x.QuantitySold > 0)
            .OrderBy(x => x.QuantitySold)
            .Take(limit)
            .ToListAsync(ct);

        return grouped;
    }

    public async Task<List<ProductMovementAnalysisDto>> GetNonMovingProductsAsync(ReportFilterViewModel filter, int days = 90, CancellationToken ct = default)
    {
        var cutoffDate = DateTime.Today.AddDays(-days);

        var activeSalesProductIds = await _context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice != null && i.SalesInvoice.IsActive && i.SalesInvoice.Status != "Cancelled" &&
                        i.SalesInvoice.InvoiceDate >= cutoffDate &&
                        (ActiveCompanyId == null || i.SalesInvoice.CompanyId == ActiveCompanyId))
            .Select(i => i.ProductId)
            .Distinct()
            .ToListAsync(ct);

        var nonMovingProducts = await GetBaseProductQuery(filter)
            .Include(p => p.Category)
            .Where(p => p.CurrentStock > 0 && !activeSalesProductIds.Contains(p.Id))
            .OrderByDescending(p => p.CurrentStock * p.PurchasePrice)
            .Select(p => new ProductMovementAnalysisDto
            {
                ProductId = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                CategoryName = p.Category != null ? p.Category.CategoryName : "General",
                CurrentStock = p.CurrentStock,
                QuantitySold = 0,
                TotalSalesAmount = 0,
                TransactionCount = 0,
                MovementClassification = "Non-Moving (Dead Stock)"
            })
            .ToListAsync(ct);

        return nonMovingProducts;
    }

    public async Task<List<ReorderSuggestionDto>> GetReorderSuggestionsAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseProductQuery(filter)
            .Include(p => p.Category)
            .Where(p => p.ReorderLevel > 0 && p.CurrentStock <= p.ReorderLevel);

        return await query
            .OrderBy(p => p.CurrentStock)
            .Select(p => new ReorderSuggestionDto
            {
                ProductId = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                CategoryName = p.Category != null ? p.Category.CategoryName : "General",
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock,
                MaximumStock = p.MaximumStock,
                ReorderLevel = p.ReorderLevel,
                SuggestedOrderQty = p.MaximumStock > p.CurrentStock ? (p.MaximumStock - p.CurrentStock) : (p.ReorderLevel * 2m - p.CurrentStock),
                PurchasePrice = p.PurchasePrice
            })
            .ToListAsync(ct);
    }

    public async Task<List<ReportSummaryCard>> GetInventorySummaryCardsAsync(ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var query = GetBaseProductQuery(filter);

        var totalProducts = await query.CountAsync(ct);
        var totalStockQty = await query.SumAsync(p => (decimal?)p.CurrentStock, ct) ?? 0m;
        var totalValuation = await query.SumAsync(p => (decimal?)(p.CurrentStock * p.PurchasePrice), ct) ?? 0m;
        var salesValuation = await query.SumAsync(p => (decimal?)(p.CurrentStock * p.SalesPrice), ct) ?? 0m;
        var lowStockCount = await query.CountAsync(p => p.CurrentStock > 0 && (p.CurrentStock <= p.MinimumStock || p.CurrentStock <= p.ReorderLevel), ct);
        var outOfStockCount = await query.CountAsync(p => p.CurrentStock <= 0, ct);

        return new List<ReportSummaryCard>
        {
            new() { Title = "Total Products", Value = totalProducts.ToString("N0"), Icon = "fa-boxes-stacked", ColorClass = "primary" },
            new() { Title = "Total Stock Qty", Value = totalStockQty.ToString("N2"), Icon = "fa-cubes", ColorClass = "info" },
            new() { Title = "Cost Valuation", Value = $"₹{totalValuation:N2}", Icon = "fa-calculator", ColorClass = "success" },
            new() { Title = "Retail Valuation", Value = $"₹{salesValuation:N2}", Icon = "fa-tags", ColorClass = "primary" },
            new() { Title = "Low Stock Alerts", Value = lowStockCount.ToString("N0"), Icon = "fa-triangle-exclamation", ColorClass = "warning" },
            new() { Title = "Out of Stock", Value = outOfStockCount.ToString("N0"), Icon = "fa-circle-xmark", ColorClass = "danger" }
        };
    }
}
