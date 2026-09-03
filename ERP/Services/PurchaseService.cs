using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;

namespace ERP.Services;

public class PurchaseService : IPurchaseService
{
    private readonly AppDbContext _context;

    public PurchaseService(AppDbContext context)
    {
        _context = context;
    }

    // Purchase Order
    public async Task<List<PurchaseOrder>> GetPurchaseOrdersAsync()
    {
        return await _context.PurchaseOrders.Include(o => o.Supplier).Where(o => o.IsActive).ToListAsync();
    }

    public async Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int id)
    {
        return await _context.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<PurchaseOrder> SavePurchaseOrderAsync(PurchaseOrder order)
    {
        if (order.SupplierId <= 0)
            throw new InvalidOperationException("Supplier selection is required.");
        if (order.Items == null || !order.Items.Any() || order.Items.Any(i => i.Quantity <= 0 || i.Rate <= 0))
            throw new InvalidOperationException("Please add at least one line item with valid quantity and rate.");

        if (order.Id == 0)
        {
            _context.PurchaseOrders.Add(order);
        }
        else
        {
            var existing = await _context.PurchaseOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == order.Id);
            if (existing != null)
            {
                _context.PurchaseOrderItems.RemoveRange(existing.Items);
                _context.Entry(existing).CurrentValues.SetValues(order);
                foreach (var item in order.Items)
                {
                    existing.Items.Add(item);
                }
                order = existing;
            }
        }
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<(bool Success, string Message)> DeletePurchaseOrderAsync(int id)
    {
        var item = await _context.PurchaseOrders.FindAsync(id);
        if (item == null) return (false, "Purchase Order not found or already removed.");

        if (item.Status == "Converted" || item.Status == "Completed")
            return (false, $"Purchase Order '{item.OrderNumber}' cannot be deleted because it is already marked as {item.Status}.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Purchase Order '{item.OrderNumber}' deleted successfully.");
    }

    public async Task UpdatePurchaseOrderStatusAsync(int id, string status)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order != null)
        {
            order.Status = status;
            await _context.SaveChangesAsync();
        }
    }

    // Goods Receipt Note
    public async Task<List<GoodsReceiptNote>> GetGRNsAsync()
    {
        return await _context.GoodsReceiptNotes.Include(g => g.Supplier).Where(g => g.IsActive).ToListAsync();
    }

    public async Task<GoodsReceiptNote?> GetGRNByIdAsync(int id)
    {
        return await _context.GoodsReceiptNotes
            .Include(g => g.Supplier)
            .Include(g => g.Items)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<GoodsReceiptNote> SaveGRNAsync(GoodsReceiptNote grn)
    {
        if (grn.SupplierId <= 0)
            throw new InvalidOperationException("Supplier selection is required.");
        if (grn.Items == null || !grn.Items.Any() || grn.Items.Any(i => i.Quantity <= 0))
            throw new InvalidOperationException("Please add at least one line item with valid quantity.");

        if (grn.Id == 0)
        {
            _context.GoodsReceiptNotes.Add(grn);
        }
        else
        {
            var existing = await _context.GoodsReceiptNotes.Include(g => g.Items).FirstOrDefaultAsync(g => g.Id == grn.Id);
            if (existing != null)
            {
                _context.GoodsReceiptNoteItems.RemoveRange(existing.Items);
                _context.Entry(existing).CurrentValues.SetValues(grn);
                foreach (var item in grn.Items)
                {
                    existing.Items.Add(item);
                }
                grn = existing;
            }
        }
        await _context.SaveChangesAsync();
        return grn;
    }

    public async Task<(bool Success, string Message)> DeleteGRNAsync(int id)
    {
        var item = await _context.GoodsReceiptNotes.FindAsync(id);
        if (item == null) return (false, "Goods Receipt Note not found or already removed.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Goods Receipt Note '{item.GRNNumber}' deleted successfully.");
    }

    // Purchase Invoice
    public async Task<List<PurchaseInvoice>> GetInvoicesAsync()
    {
        return await _context.PurchaseInvoices.Include(i => i.Supplier).Where(i => i.IsActive).ToListAsync();
    }

    public async Task<PurchaseInvoice?> GetInvoiceByIdAsync(int id)
    {
        return await _context.PurchaseInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Items).ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<PurchaseInvoice> SaveInvoiceAsync(PurchaseInvoice invoice)
    {
        if (invoice.SupplierId <= 0)
            throw new InvalidOperationException("Supplier selection is required.");
        if (invoice.Items == null || !invoice.Items.Any() || invoice.Items.Any(i => i.Quantity <= 0 || i.Rate <= 0))
            throw new InvalidOperationException("Please add at least one valid line item with quantity > 0 and rate > 0.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var supplier = await _context.Suppliers.FindAsync(invoice.SupplierId)
                ?? throw new InvalidOperationException("Selected supplier was not found.");

            foreach (var item in invoice.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId)
                    ?? throw new InvalidOperationException("One or more selected products were not found.");

                item.ProductName = string.IsNullOrWhiteSpace(item.ProductName) ? product.ProductName : item.ProductName;
                item.Product = null;
                item.PurchaseInvoice = null;
                if (!invoice.WithGST)
                {
                    item.TaxPercentage = 0;
                    item.CGSTAmount = 0;
                    item.SGSTAmount = 0;
                    item.IGSTAmount = 0;
                    item.TaxAmount = 0;
                    item.Amount = item.Quantity * item.Rate;
                }
                else
                {
                    item.TaxPercentage = item.TaxPercentage == 0 ? product.GSTPercentage : item.TaxPercentage;
                    item.TaxAmount = item.TaxAmount == 0 ? item.Quantity * item.Rate * (item.TaxPercentage / 100) : item.TaxAmount;
                    item.Amount = item.Amount == 0 ? (item.Quantity * item.Rate) + item.TaxAmount : item.Amount;
                }
            }

            if (!invoice.WithGST)
            {
                invoice.TaxAmount = 0;
            }
            invoice.SubTotal = invoice.SubTotal == 0 ? invoice.Items.Sum(i => i.Quantity * i.Rate) : invoice.SubTotal;
            invoice.TaxAmount = !invoice.WithGST ? 0 : (invoice.TaxAmount == 0 ? invoice.Items.Sum(i => i.TaxAmount) : invoice.TaxAmount);
            invoice.GrandTotal = invoice.SubTotal + invoice.TaxAmount - invoice.DiscountAmount + invoice.RoundOff;
            invoice.BalanceAmount = invoice.GrandTotal - invoice.PaidAmount;

            invoice.Supplier = null;

            if (invoice.Id == 0)
            {
                _context.PurchaseInvoices.Add(invoice);
                await _context.SaveChangesAsync();
            }
            else
            {
                var existing = await _context.PurchaseInvoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == invoice.Id);
                if (existing != null)
                {
                    var oldTransactions = await _context.StockTransactions
                        .Where(t => t.TransactionType == "Purchase" && t.ReferenceNumber == existing.InvoiceNumber)
                        .ToListAsync();
                    _context.StockTransactions.RemoveRange(oldTransactions);

                    foreach (var item in existing.Items)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.CurrentStock -= item.Quantity;
                        }
                    }

                    _context.PurchaseInvoiceItems.RemoveRange(existing.Items);
                    _context.Entry(existing).CurrentValues.SetValues(invoice);
                    foreach (var item in invoice.Items)
                    {
                        existing.Items.Add(item);
                    }
                    invoice = existing;
                    await _context.SaveChangesAsync();
                }
            }

            foreach (var item in invoice.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.CurrentStock += item.Quantity;
                    _context.StockTransactions.Add(new StockTransaction
                    {
                        TransactionDate = invoice.InvoiceDate,
                        TransactionType = "Purchase",
                        ReferenceNumber = invoice.InvoiceNumber,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Rate = item.Rate,
                        Remarks = $"Purchase Invoice {invoice.InvoiceNumber}"
                    });
                }
            }

            var supplierLedger = await _context.Ledgers.FirstOrDefaultAsync(l => l.LedgerName.Contains(supplier.SupplierName) || (!string.IsNullOrEmpty(supplier.SupplierCode) && l.LedgerCode == supplier.SupplierCode));
            if (supplierLedger == null)
            {
                supplierLedger = new Ledger
                {
                    LedgerCode = !string.IsNullOrWhiteSpace(supplier.SupplierCode) ? supplier.SupplierCode : $"SUPP-{supplier.Id}",
                    LedgerName = supplier.SupplierName,
                    AccountGroupId = (await _context.AccountGroups.FirstOrDefaultAsync(g => g.GroupName == "Sundry Creditors"))?.Id ?? 1,
                    OpeningBalance = 0,
                    BalanceType = "Cr"
                };
                _context.Ledgers.Add(supplierLedger);
                await _context.SaveChangesAsync();
            }

            var purchaseLedger = await _context.Ledgers.FirstOrDefaultAsync(l => l.LedgerName == "Purchase Account" || l.LedgerCode == "PURCHASE");
            if (purchaseLedger == null)
            {
                purchaseLedger = new Ledger
                {
                    LedgerCode = "PURCHASE",
                    LedgerName = "Purchase Account",
                    AccountGroupId = (await _context.AccountGroups.FirstOrDefaultAsync(g => g.GroupName == "Purchase Accounts"))?.Id ?? 1,
                    OpeningBalance = 0,
                    BalanceType = "Dr"
                };
                _context.Ledgers.Add(purchaseLedger);
                await _context.SaveChangesAsync();
            }

            var existingVoucher = await _context.Vouchers.Include(v => v.Items).FirstOrDefaultAsync(v => v.ReferenceNumber == invoice.InvoiceNumber && v.VoucherType == "Journal");
            if (existingVoucher != null)
            {
                _context.Vouchers.Remove(existingVoucher);
                await _context.SaveChangesAsync();
            }

            var voucher = new Voucher
            {
                Id = 0,
                VoucherNumber = "JV-" + invoice.InvoiceNumber,
                VoucherDate = invoice.InvoiceDate,
                VoucherType = "Journal",
                ReferenceNumber = invoice.InvoiceNumber,
                Narration = $"Purchase invoice {invoice.InvoiceNumber} generated from supplier {supplier.SupplierName}",
                TotalAmount = invoice.GrandTotal
            };

            voucher.Items.Add(new VoucherItem
            {
                Id = 0,
                LedgerId = purchaseLedger.Id,
                DebitAmount = invoice.GrandTotal,
                CreditAmount = 0,
                Particulars = $"To Purchase Dr",
                SortOrder = 1
            });

            voucher.Items.Add(new VoucherItem
            {
                Id = 0,
                LedgerId = supplierLedger.Id,
                DebitAmount = 0,
                CreditAmount = invoice.GrandTotal,
                Particulars = $"By Credit to Supplier",
                SortOrder = 2
            });

            _context.Vouchers.Add(voucher);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        return invoice;
    }

    public async Task<(bool Success, string Message)> DeleteInvoiceAsync(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var item = await _context.PurchaseInvoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return (false, "Purchase Invoice not found or already removed.");

            item.IsActive = false;
            foreach (var line in item.Items)
            {
                var product = await _context.Products.FindAsync(line.ProductId);
                if (product != null)
                {
                    product.CurrentStock -= line.Quantity;
                }
            }

            var stockTxns = await _context.StockTransactions
                .Where(t => t.TransactionType == "Purchase" && t.ReferenceNumber == item.InvoiceNumber)
                .ToListAsync();
            _context.StockTransactions.RemoveRange(stockTxns);

            var voucher = await _context.Vouchers.Include(v => v.Items).FirstOrDefaultAsync(v => v.ReferenceNumber == item.InvoiceNumber);
            if (voucher != null)
            {
                _context.Vouchers.Remove(voucher);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, $"Purchase Invoice '{item.InvoiceNumber}' deleted and stock reverted successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Failed to delete invoice: {ex.Message}");
        }
    }

    // Purchase Return
    public async Task<List<PurchaseReturn>> GetReturnsAsync()
    {
        return await _context.PurchaseReturns.Include(r => r.Supplier).Where(r => r.IsActive).ToListAsync();
    }

    public async Task<PurchaseReturn?> GetReturnByIdAsync(int id)
    {
        return await _context.PurchaseReturns
            .Include(r => r.Supplier)
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<PurchaseReturn> SaveReturnAsync(PurchaseReturn purchaseReturn)
    {
        if (purchaseReturn.SupplierId <= 0)
            throw new InvalidOperationException("Supplier selection is required.");
        if (purchaseReturn.Items == null || !purchaseReturn.Items.Any() || purchaseReturn.Items.Any(i => i.Quantity <= 0))
            throw new InvalidOperationException("Please add at least one line item with valid return quantity.");

        if (purchaseReturn.Id == 0)
        {
            _context.PurchaseReturns.Add(purchaseReturn);
        }
        else
        {
            var existing = await _context.PurchaseReturns.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == purchaseReturn.Id);
            if (existing != null)
            {
                _context.PurchaseReturnItems.RemoveRange(existing.Items);
                _context.Entry(existing).CurrentValues.SetValues(purchaseReturn);
                foreach (var item in purchaseReturn.Items)
                {
                    existing.Items.Add(item);
                }
                purchaseReturn = existing;
            }
        }
        await _context.SaveChangesAsync();
        return purchaseReturn;
    }

    public async Task<(bool Success, string Message)> DeleteReturnAsync(int id)
    {
        var item = await _context.PurchaseReturns.FindAsync(id);
        if (item == null) return (false, "Purchase Return not found or already removed.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Purchase Return '{item.ReturnNumber}' deleted successfully.");
    }
}
