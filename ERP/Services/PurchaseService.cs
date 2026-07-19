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

    public async Task DeletePurchaseOrderAsync(int id)
    {
        var item = await _context.PurchaseOrders.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
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

    public async Task DeleteGRNAsync(int id)
    {
        var item = await _context.GoodsReceiptNotes.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
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
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            invoice.Supplier = await _context.Suppliers.FindAsync(invoice.SupplierId)
                ?? throw new InvalidOperationException("Selected supplier was not found.");

            foreach (var item in invoice.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId)
                    ?? throw new InvalidOperationException("One or more selected products were not found.");

                item.ProductName = string.IsNullOrWhiteSpace(item.ProductName) ? product.ProductName : item.ProductName;
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
                    // Revert old stock transactions if updating
                    var oldTransactions = await _context.StockTransactions
                        .Where(t => t.TransactionType == "Purchase" && t.ReferenceNumber == existing.InvoiceNumber)
                        .ToListAsync();
                    _context.StockTransactions.RemoveRange(oldTransactions);

                    // De-stock quantities
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

            // Increase stock for purchase items & add StockTransactions
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

            // Generate Accounting Voucher (Purchase Account Dr, Supplier Account Cr)
            var supplierLedger = await _context.Ledgers.FirstOrDefaultAsync(l => l.LedgerName.Contains(invoice.Supplier!.SupplierName) || l.LedgerCode == invoice.Supplier.SupplierCode);
            if (supplierLedger == null)
            {
                supplierLedger = new Ledger
                {
                    LedgerCode = invoice.Supplier.SupplierCode,
                    LedgerName = invoice.Supplier.SupplierName,
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

            var existingVoucher = await _context.Vouchers.Include(v => v.Items).FirstOrDefaultAsync(v => v.ReferenceNumber == invoice.InvoiceNumber && v.VoucherType == "Purchase");
            if (existingVoucher != null)
            {
                _context.Vouchers.Remove(existingVoucher);
            }

            var voucher = new Voucher
            {
                VoucherNumber = "JV-" + invoice.InvoiceNumber,
                VoucherDate = invoice.InvoiceDate,
                VoucherType = "Journal",
                ReferenceNumber = invoice.InvoiceNumber,
                Narration = $"Purchase invoice {invoice.InvoiceNumber} generated from supplier {invoice.Supplier.SupplierName}",
                TotalAmount = invoice.GrandTotal
            };

            voucher.Items.Add(new VoucherItem
            {
                LedgerId = purchaseLedger.Id,
                DebitAmount = invoice.GrandTotal,
                CreditAmount = 0,
                Particulars = $"To Purchase Dr",
                SortOrder = 1
            });

            voucher.Items.Add(new VoucherItem
            {
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

    public async Task DeleteInvoiceAsync(int id)
    {
        var item = await _context.PurchaseInvoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id);
        if (item != null)
        {
            item.IsActive = false;
            // Deduct stock back
            foreach (var line in item.Items)
            {
                var product = await _context.Products.FindAsync(line.ProductId);
                if (product != null)
                {
                    product.CurrentStock -= line.Quantity;
                }
            }
            await _context.SaveChangesAsync();
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

    public async Task DeleteReturnAsync(int id)
    {
        var item = await _context.PurchaseReturns.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }
}
