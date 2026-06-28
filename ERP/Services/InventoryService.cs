using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;

namespace ERP.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    // Stock Opening
    public async Task<List<Product>> GetProductsForOpeningStockAsync()
    {
        return await _context.Products
            .Include(p => p.Warehouse)
            .Include(p => p.Unit)
            .Where(p => p.IsActive)
            .ToListAsync();
    }

    public async Task UpdateOpeningStockAsync(int productId, decimal quantity)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product != null)
        {
            // Calculate stock difference to adjust current stock
            var difference = quantity - product.OpeningStock;
            product.OpeningStock = quantity;
            product.CurrentStock += difference;

            // Log or update StockTransaction for Opening Stock
            var refNo = $"OP-{product.ProductCode}";
            var existingTx = await _context.StockTransactions
                .FirstOrDefaultAsync(t => t.TransactionType == "Opening" && t.ReferenceNumber == refNo && t.ProductId == productId);

            if (existingTx != null)
            {
                existingTx.Quantity = quantity;
                existingTx.TransactionDate = DateTime.Now;
            }
            else
            {
                _context.StockTransactions.Add(new StockTransaction
                {
                    ProductId = productId,
                    Quantity = quantity,
                    TransactionType = "Opening",
                    ReferenceNumber = refNo,
                    TransactionDate = DateTime.Now,
                    Remarks = $"Opening Stock registered for {product.ProductName}",
                    Rate = product.PurchasePrice,
                    WarehouseId = product.WarehouseId
                });
            }

            await _context.SaveChangesAsync();
        }
    }

    // Stock Transfer
    public async Task<List<StockTransfer>> GetStockTransfersAsync()
    {
        return await _context.StockTransfers
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Where(t => t.IsActive)
            .ToListAsync();
    }

    public async Task<StockTransfer?> GetStockTransferByIdAsync(int id)
    {
        return await _context.StockTransfers
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<StockTransfer> SaveStockTransferAsync(StockTransfer transfer)
    {
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            if (transfer.Id == 0)
            {
                _context.StockTransfers.Add(transfer);
                await _context.SaveChangesAsync();
            }
            else
            {
                var existing = await _context.StockTransfers
                    .Include(t => t.Items)
                    .FirstOrDefaultAsync(t => t.Id == transfer.Id);

                if (existing != null)
                {
                    // Revert old stock transactions
                    var oldTxs = await _context.StockTransactions
                        .Where(t => t.TransactionType == "Transfer" && t.ReferenceNumber == existing.TransferNumber)
                        .ToListAsync();
                    _context.StockTransactions.RemoveRange(oldTxs);

                    _context.StockTransferItems.RemoveRange(existing.Items);
                    _context.Entry(existing).CurrentValues.SetValues(transfer);
                    foreach (var item in transfer.Items)
                    {
                        existing.Items.Add(item);
                    }
                    transfer = existing;
                    await _context.SaveChangesAsync();
                }
            }

            // Create Stock Transactions for the Transfer
            foreach (var item in transfer.Items)
            {
                // Note: Net current stock doesn't change since it's just a warehouse transfer,
                // but we record the transfer transactions for stock card reporting.
                _context.StockTransactions.Add(new StockTransaction
                {
                    TransactionDate = transfer.TransferDate,
                    TransactionType = "Transfer-Out",
                    ReferenceNumber = transfer.TransferNumber,
                    ProductId = item.ProductId,
                    Quantity = -item.Quantity,
                    WarehouseId = transfer.FromWarehouseId,
                    Remarks = $"Transfer to {transfer.ToWarehouse?.WarehouseName ?? "other warehouse"}"
                });

                _context.StockTransactions.Add(new StockTransaction
                {
                    TransactionDate = transfer.TransferDate,
                    TransactionType = "Transfer-In",
                    ReferenceNumber = transfer.TransferNumber,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    WarehouseId = transfer.ToWarehouseId,
                    Remarks = $"Transfer from {transfer.FromWarehouse?.WarehouseName ?? "other warehouse"}"
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return transfer;
    }

    public async Task DeleteStockTransferAsync(int id)
    {
        var transfer = await _context.StockTransfers.FindAsync(id);
        if (transfer != null)
        {
            transfer.IsActive = false;

            // Remove stock transactions
            var txs = await _context.StockTransactions
                .Where(t => (t.TransactionType == "Transfer-In" || t.TransactionType == "Transfer-Out") && t.ReferenceNumber == transfer.TransferNumber)
                .ToListAsync();
            _context.StockTransactions.RemoveRange(txs);

            await _context.SaveChangesAsync();
        }
    }

    // Stock Adjustment
    public async Task<List<StockAdjustment>> GetStockAdjustmentsAsync()
    {
        return await _context.StockAdjustments
            .Include(a => a.Warehouse)
            .Where(a => a.IsActive)
            .ToListAsync();
    }

    public async Task<StockAdjustment?> GetStockAdjustmentByIdAsync(int id)
    {
        return await _context.StockAdjustments
            .Include(a => a.Warehouse)
            .Include(a => a.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<StockAdjustment> SaveStockAdjustmentAsync(StockAdjustment adjustment)
    {
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            if (adjustment.Id == 0)
            {
                _context.StockAdjustments.Add(adjustment);
                await _context.SaveChangesAsync();
            }
            else
            {
                var existing = await _context.StockAdjustments
                    .Include(a => a.Items)
                    .FirstOrDefaultAsync(a => a.Id == adjustment.Id);

                if (existing != null)
                {
                    // Revert old stock levels & transactions
                    var oldTxs = await _context.StockTransactions
                        .Where(t => t.TransactionType == "Adjustment" && t.ReferenceNumber == existing.AdjustmentNumber)
                        .ToListAsync();
                    _context.StockTransactions.RemoveRange(oldTxs);

                    foreach (var item in existing.Items)
                    {
                        var prod = await _context.Products.FindAsync(item.ProductId);
                        if (prod != null)
                        {
                            if (existing.AdjustmentType == "Addition")
                                prod.CurrentStock -= item.Quantity;
                            else
                                prod.CurrentStock += item.Quantity;
                        }
                    }

                    _context.StockAdjustmentItems.RemoveRange(existing.Items);
                    _context.Entry(existing).CurrentValues.SetValues(adjustment);
                    foreach (var item in adjustment.Items)
                    {
                        existing.Items.Add(item);
                    }
                    adjustment = existing;
                    await _context.SaveChangesAsync();
                }
            }

            // Apply stock levels and add Transactions
            foreach (var item in adjustment.Items)
            {
                var prod = await _context.Products.FindAsync(item.ProductId);
                if (prod != null)
                {
                    var qty = adjustment.AdjustmentType == "Addition" ? item.Quantity : -item.Quantity;
                    prod.CurrentStock += qty;

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        TransactionDate = adjustment.AdjustmentDate,
                        TransactionType = "Adjustment",
                        ReferenceNumber = adjustment.AdjustmentNumber,
                        ProductId = item.ProductId,
                        Quantity = qty,
                        Rate = item.Rate,
                        WarehouseId = adjustment.WarehouseId,
                        Remarks = $"Stock Adjustment ({adjustment.AdjustmentType}): {adjustment.Remarks}"
                    });
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return adjustment;
    }

    public async Task DeleteStockAdjustmentAsync(int id)
    {
        var adjustment = await _context.StockAdjustments.Include(a => a.Items).FirstOrDefaultAsync(a => a.Id == id);
        if (adjustment != null)
        {
            adjustment.IsActive = false;

            // Revert stock levels
            foreach (var item in adjustment.Items)
            {
                var prod = await _context.Products.FindAsync(item.ProductId);
                if (prod != null)
                {
                    if (adjustment.AdjustmentType == "Addition")
                        prod.CurrentStock -= item.Quantity;
                    else
                        prod.CurrentStock += item.Quantity;
                }
            }

            // Remove transactions
            var txs = await _context.StockTransactions
                .Where(t => t.TransactionType == "Adjustment" && t.ReferenceNumber == adjustment.AdjustmentNumber)
                .ToListAsync();
            _context.StockTransactions.RemoveRange(txs);

            await _context.SaveChangesAsync();
        }
    }

    // Physical Stock Verification
    public async Task<List<PhysicalStockVerification>> GetPhysicalStockVerificationsAsync()
    {
        return await _context.PhysicalStockVerifications
            .Include(v => v.Warehouse)
            .Where(v => v.IsActive)
            .ToListAsync();
    }

    public async Task<PhysicalStockVerification?> GetPhysicalStockVerificationByIdAsync(int id)
    {
        return await _context.PhysicalStockVerifications
            .Include(v => v.Warehouse)
            .Include(v => v.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<PhysicalStockVerification> SavePhysicalStockVerificationAsync(PhysicalStockVerification verification)
    {
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            if (verification.Id == 0)
            {
                _context.PhysicalStockVerifications.Add(verification);
                await _context.SaveChangesAsync();
            }
            else
            {
                var existing = await _context.PhysicalStockVerifications
                    .Include(v => v.Items)
                    .FirstOrDefaultAsync(v => v.Id == verification.Id);

                if (existing != null)
                {
                    // Revert old variance modifications from product current stock
                    var oldTxs = await _context.StockTransactions
                        .Where(t => t.TransactionType == "Verification" && t.ReferenceNumber == existing.VerificationNumber)
                        .ToListAsync();
                    _context.StockTransactions.RemoveRange(oldTxs);

                    foreach (var item in existing.Items)
                    {
                        var prod = await _context.Products.FindAsync(item.ProductId);
                        if (prod != null)
                        {
                            prod.CurrentStock -= item.Variance;
                        }
                    }

                    _context.PhysicalStockVerificationItems.RemoveRange(existing.Items);
                    _context.Entry(existing).CurrentValues.SetValues(verification);
                    foreach (var item in verification.Items)
                    {
                        existing.Items.Add(item);
                    }
                    verification = existing;
                    await _context.SaveChangesAsync();
                }
            }

            // Apply verification variance and create transactions
            foreach (var item in verification.Items)
            {
                var prod = await _context.Products.FindAsync(item.ProductId);
                if (prod != null)
                {
                    // Force variance recalculation: variance = physical - book stock
                    item.BookStock = prod.CurrentStock;
                    item.Variance = item.PhysicalStock - item.BookStock;

                    prod.CurrentStock = item.PhysicalStock;

                    if (item.Variance != 0)
                    {
                        _context.StockTransactions.Add(new StockTransaction
                        {
                            TransactionDate = verification.VerificationDate,
                            TransactionType = "Verification",
                            ReferenceNumber = verification.VerificationNumber,
                            ProductId = item.ProductId,
                            Quantity = item.Variance,
                            WarehouseId = verification.WarehouseId,
                            Rate = prod.PurchasePrice,
                            Remarks = $"Stock Verification Variance (Book: {item.BookStock}, Physical: {item.PhysicalStock})"
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return verification;
    }

    public async Task DeletePhysicalStockVerificationAsync(int id)
    {
        var verification = await _context.PhysicalStockVerifications
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (verification != null)
        {
            verification.IsActive = false;

            // Revert stock variance adjustments
            foreach (var item in verification.Items)
            {
                var prod = await _context.Products.FindAsync(item.ProductId);
                if (prod != null)
                {
                    prod.CurrentStock -= item.Variance;
                }
            }

            // Remove transactions
            var txs = await _context.StockTransactions
                .Where(t => t.TransactionType == "Verification" && t.ReferenceNumber == verification.VerificationNumber)
                .ToListAsync();
            _context.StockTransactions.RemoveRange(txs);

            await _context.SaveChangesAsync();
        }
    }
}
