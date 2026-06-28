using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;

namespace ERP.Services;

public class SalesService : ISalesService
{
    private readonly AppDbContext _context;

    public SalesService(AppDbContext context)
    {
        _context = context;
    }

    // Sales Quotation
    public async Task<List<SalesQuotation>> GetQuotationsAsync()
    {
        return await _context.SalesQuotations.Include(q => q.Customer).Where(q => q.IsActive).ToListAsync();
    }

    public async Task<SalesQuotation?> GetQuotationByIdAsync(int id)
    {
        return await _context.SalesQuotations
            .Include(q => q.Customer)
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<SalesQuotation> SaveQuotationAsync(SalesQuotation quotation)
    {
        if (quotation.Id == 0)
        {
            _context.SalesQuotations.Add(quotation);
        }
        else
        {
            var existing = await _context.SalesQuotations.Include(q => q.Items).FirstOrDefaultAsync(q => q.Id == quotation.Id);
            if (existing != null)
            {
                _context.SalesQuotationItems.RemoveRange(existing.Items);
                _context.Entry(existing).CurrentValues.SetValues(quotation);
                foreach (var item in quotation.Items)
                {
                    existing.Items.Add(item);
                }
                quotation = existing;
            }
        }
        await _context.SaveChangesAsync();
        return quotation;
    }

    public async Task DeleteQuotationAsync(int id)
    {
        var item = await _context.SalesQuotations.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    // Sales Order
    public async Task<List<SalesOrder>> GetSalesOrdersAsync()
    {
        return await _context.SalesOrders.Include(o => o.Customer).Where(o => o.IsActive).ToListAsync();
    }

    public async Task<SalesOrder?> GetSalesOrderByIdAsync(int id)
    {
        return await _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<SalesOrder> SaveSalesOrderAsync(SalesOrder order)
    {
        if (order.Id == 0)
        {
            _context.SalesOrders.Add(order);
        }
        else
        {
            var existing = await _context.SalesOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == order.Id);
            if (existing != null)
            {
                _context.SalesOrderItems.RemoveRange(existing.Items);
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

    public async Task DeleteSalesOrderAsync(int id)
    {
        var item = await _context.SalesOrders.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateSalesOrderStatusAsync(int id, string status)
    {
        var order = await _context.SalesOrders.FindAsync(id);
        if (order != null)
        {
            order.Status = status;
            await _context.SaveChangesAsync();
        }
    }

    // Delivery Challan
    public async Task<List<DeliveryChallan>> GetDeliveryChallansAsync()
    {
        return await _context.DeliveryChallans.Include(c => c.Customer).Where(c => c.IsActive).ToListAsync();
    }

    public async Task<DeliveryChallan?> GetDeliveryChallanByIdAsync(int id)
    {
        return await _context.DeliveryChallans
            .Include(c => c.Customer)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<DeliveryChallan> SaveDeliveryChallanAsync(DeliveryChallan challan)
    {
        if (challan.Id == 0)
        {
            _context.DeliveryChallans.Add(challan);
        }
        else
        {
            var existing = await _context.DeliveryChallans.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == challan.Id);
            if (existing != null)
            {
                _context.DeliveryChallanItems.RemoveRange(existing.Items);
                _context.Entry(existing).CurrentValues.SetValues(challan);
                foreach (var item in challan.Items)
                {
                    existing.Items.Add(item);
                }
                challan = existing;
            }
        }
        await _context.SaveChangesAsync();
        return challan;
    }

    public async Task DeleteDeliveryChallanAsync(int id)
    {
        var item = await _context.DeliveryChallans.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    // Sales Invoice
    public async Task<List<SalesInvoice>> GetInvoicesAsync()
    {
        return await _context.SalesInvoices.Include(i => i.Customer).Where(i => i.IsActive).ToListAsync();
    }

    public async Task<SalesInvoice?> GetInvoiceByIdAsync(int id)
    {
        return await _context.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<SalesInvoice> SaveInvoiceAsync(SalesInvoice invoice)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            invoice.Customer = await _context.Customers.FindAsync(invoice.CustomerId)
                ?? throw new InvalidOperationException("Selected customer was not found.");

            foreach (var item in invoice.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId)
                    ?? throw new InvalidOperationException("One or more selected products were not found.");

                item.ProductName = string.IsNullOrWhiteSpace(item.ProductName) ? product.ProductName : item.ProductName;
                item.TaxPercentage = item.TaxPercentage == 0 ? product.GSTPercentage : item.TaxPercentage;
                item.TaxAmount = item.TaxAmount == 0 ? item.Quantity * item.Rate * (item.TaxPercentage / 100) : item.TaxAmount;
                item.Amount = item.Amount == 0 ? (item.Quantity * item.Rate) + item.TaxAmount : item.Amount;
            }

            invoice.SubTotal = invoice.SubTotal == 0 ? invoice.Items.Sum(i => i.Quantity * i.Rate) : invoice.SubTotal;
            invoice.TaxAmount = invoice.TaxAmount == 0 ? invoice.Items.Sum(i => i.TaxAmount) : invoice.TaxAmount;
            invoice.GrandTotal = invoice.GrandTotal == 0 ? invoice.SubTotal + invoice.TaxAmount - invoice.DiscountAmount + invoice.RoundOff : invoice.GrandTotal;
            invoice.BalanceAmount = invoice.BalanceAmount == 0 ? invoice.GrandTotal - invoice.PaidAmount : invoice.BalanceAmount;

            if (invoice.Id == 0)
            {
                _context.SalesInvoices.Add(invoice);
                await _context.SaveChangesAsync();
            }
            else
            {
                var existing = await _context.SalesInvoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == invoice.Id);
                if (existing != null)
                {
                    // Revert old stock transactions if updating
                    var oldTransactions = await _context.StockTransactions
                        .Where(t => t.TransactionType == "Sales" && t.ReferenceNumber == existing.InvoiceNumber)
                        .ToListAsync();
                    _context.StockTransactions.RemoveRange(oldTransactions);

                    // Restock quantities
                    foreach (var item in existing.Items)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.CurrentStock += item.Quantity;
                        }
                    }

                    _context.SalesInvoiceItems.RemoveRange(existing.Items);
                    _context.Entry(existing).CurrentValues.SetValues(invoice);
                    foreach (var item in invoice.Items)
                    {
                        existing.Items.Add(item);
                    }
                    invoice = existing;
                    await _context.SaveChangesAsync();
                }
            }

            // Deduct stock for invoice items & add StockTransactions
            foreach (var item in invoice.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.CurrentStock -= item.Quantity;
                    _context.StockTransactions.Add(new StockTransaction
                    {
                        TransactionDate = invoice.InvoiceDate,
                        TransactionType = "Sales",
                        ReferenceNumber = invoice.InvoiceNumber,
                        ProductId = item.ProductId,
                        Quantity = -item.Quantity,
                        Rate = item.Rate,
                        Remarks = $"Sales Invoice {invoice.InvoiceNumber}"
                    });
                }
            }

            // Generate Accounting Voucher (Sales Account Cr, Customer Account Dr)
            var customerLedger = await _context.Ledgers.FirstOrDefaultAsync(l => l.LedgerName.Contains(invoice.Customer!.CustomerName) || l.LedgerCode == invoice.Customer.CustomerCode);
            if (customerLedger == null)
            {
                // Auto create customer ledger if not exist
                customerLedger = new Ledger
                {
                    LedgerCode = invoice.Customer.CustomerCode,
                    LedgerName = invoice.Customer.CustomerName,
                    AccountGroupId = (await _context.AccountGroups.FirstOrDefaultAsync(g => g.GroupName == "Sundry Debtors"))?.Id ?? 1,
                    OpeningBalance = 0,
                    BalanceType = "Dr"
                };
                _context.Ledgers.Add(customerLedger);
                await _context.SaveChangesAsync();
            }

            var salesLedger = await _context.Ledgers.FirstOrDefaultAsync(l => l.LedgerName == "Sales Account" || l.LedgerCode == "SALES");
            if (salesLedger == null)
            {
                salesLedger = new Ledger
                {
                    LedgerCode = "SALES",
                    LedgerName = "Sales Account",
                    AccountGroupId = (await _context.AccountGroups.FirstOrDefaultAsync(g => g.GroupName == "Sales Accounts"))?.Id ?? 1,
                    OpeningBalance = 0,
                    BalanceType = "Cr"
                };
                _context.Ledgers.Add(salesLedger);
                await _context.SaveChangesAsync();
            }

            // Check if voucher exists
            var existingVoucher = await _context.Vouchers.Include(v => v.Items).FirstOrDefaultAsync(v => v.ReferenceNumber == invoice.InvoiceNumber && v.VoucherType == "Journal");
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
                Narration = $"Sales invoice {invoice.InvoiceNumber} generated for customer {invoice.Customer.CustomerName}",
                TotalAmount = invoice.GrandTotal
            };

            voucher.Items.Add(new VoucherItem
            {
                LedgerId = customerLedger.Id,
                DebitAmount = invoice.GrandTotal,
                CreditAmount = 0,
                Particulars = $"To Customer Sales Outstanding",
                SortOrder = 1
            });

            voucher.Items.Add(new VoucherItem
            {
                LedgerId = salesLedger.Id,
                DebitAmount = 0,
                CreditAmount = invoice.GrandTotal,
                Particulars = $"By Credit to Sales",
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
        var item = await _context.SalesInvoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id);
        if (item != null)
        {
            item.IsActive = false;
            // Restore stock
            foreach (var line in item.Items)
            {
                var product = await _context.Products.FindAsync(line.ProductId);
                if (product != null)
                {
                    product.CurrentStock += line.Quantity;
                }
            }
            await _context.SaveChangesAsync();
        }
    }

    // Sales Return
    public async Task<List<SalesReturn>> GetReturnsAsync()
    {
        return await _context.SalesReturns.Include(r => r.Customer).Where(r => r.IsActive).ToListAsync();
    }

    public async Task<SalesReturn?> GetReturnByIdAsync(int id)
    {
        return await _context.SalesReturns
            .Include(r => r.Customer)
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<SalesReturn> SaveReturnAsync(SalesReturn salesReturn)
    {
        if (salesReturn.Id == 0)
        {
            _context.SalesReturns.Add(salesReturn);
        }
        else
        {
            var existing = await _context.SalesReturns.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == salesReturn.Id);
            if (existing != null)
            {
                _context.SalesReturnItems.RemoveRange(existing.Items);
                _context.Entry(existing).CurrentValues.SetValues(salesReturn);
                foreach (var item in salesReturn.Items)
                {
                    existing.Items.Add(item);
                }
                salesReturn = existing;
            }
        }
        await _context.SaveChangesAsync();
        return salesReturn;
    }

    public async Task DeleteReturnAsync(int id)
    {
        var item = await _context.SalesReturns.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }
}
