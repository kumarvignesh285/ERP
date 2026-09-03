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
        if (quotation.CustomerId <= 0)
            throw new InvalidOperationException("Customer selection is required.");
        if (quotation.Items == null || !quotation.Items.Any() || quotation.Items.Any(i => i.Quantity <= 0 || i.Rate <= 0))
            throw new InvalidOperationException("Please add at least one line item with valid quantity and rate.");

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

    public async Task<(bool Success, string Message)> DeleteQuotationAsync(int id)
    {
        var item = await _context.SalesQuotations.FindAsync(id);
        if (item == null) return (false, "Sales Quotation not found or already removed.");

        if (item.Status == "Converted" || item.Status == "Accepted")
            return (false, $"Quotation '{item.QuotationNumber}' cannot be deleted because it is already marked as {item.Status}.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Sales Quotation '{item.QuotationNumber}' deleted successfully.");
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
        if (order.CustomerId <= 0)
            throw new InvalidOperationException("Customer selection is required.");
        if (order.Items == null || !order.Items.Any() || order.Items.Any(i => i.Quantity <= 0 || i.Rate <= 0))
            throw new InvalidOperationException("Please add at least one line item with valid quantity and rate.");

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

    public async Task<(bool Success, string Message)> DeleteSalesOrderAsync(int id)
    {
        var item = await _context.SalesOrders.FindAsync(id);
        if (item == null) return (false, "Sales Order not found or already removed.");

        if (item.Status == "Converted" || item.Status == "Completed")
            return (false, $"Sales Order '{item.OrderNumber}' cannot be deleted because it has already been {item.Status}.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Sales Order '{item.OrderNumber}' deleted successfully.");
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
        if (challan.CustomerId <= 0)
            throw new InvalidOperationException("Customer selection is required.");
        if (challan.Items == null || !challan.Items.Any() || challan.Items.Any(i => i.Quantity <= 0))
            throw new InvalidOperationException("Please add at least one line item with valid quantity.");

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

    public async Task<(bool Success, string Message)> DeleteDeliveryChallanAsync(int id)
    {
        var item = await _context.DeliveryChallans.FindAsync(id);
        if (item == null) return (false, "Delivery Challan not found or already removed.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Delivery Challan '{item.ChallanNumber}' deleted successfully.");
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
            .Include(i => i.Items).ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<SalesInvoice> SaveInvoiceAsync(SalesInvoice invoice)
    {
        if (invoice.CustomerId <= 0)
            throw new InvalidOperationException("Customer selection is required.");
        if (invoice.Items == null || !invoice.Items.Any() || invoice.Items.Any(i => i.Quantity <= 0 || i.Rate <= 0))
            throw new InvalidOperationException("Please add at least one valid invoice item with quantity > 0 and rate > 0.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var customer = await _context.Customers.FindAsync(invoice.CustomerId)
                ?? throw new InvalidOperationException("Selected customer was not found.");

            foreach (var item in invoice.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId)
                    ?? throw new InvalidOperationException("One or more selected products were not found.");

                item.ProductName = string.IsNullOrWhiteSpace(item.ProductName) ? product.ProductName : item.ProductName;
                item.Product = null;
                item.SalesInvoice = null;
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

            invoice.Customer = null;

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
                    var oldTransactions = await _context.StockTransactions
                        .Where(t => t.TransactionType == "Sales" && t.ReferenceNumber == existing.InvoiceNumber)
                        .ToListAsync();
                    _context.StockTransactions.RemoveRange(oldTransactions);

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

            var customerLedger = await _context.Ledgers.FirstOrDefaultAsync(l => l.LedgerName.Contains(customer.CustomerName) || (!string.IsNullOrEmpty(customer.CustomerCode) && l.LedgerCode == customer.CustomerCode));
            if (customerLedger == null)
            {
                customerLedger = new Ledger
                {
                    LedgerCode = !string.IsNullOrWhiteSpace(customer.CustomerCode) ? customer.CustomerCode : $"CUST-{customer.Id}",
                    LedgerName = customer.CustomerName,
                    AccountGroupId = (await _context.AccountGroups.FirstOrDefaultAsync(g => g.GroupName == "Sundry Debtors"))?.Id ?? 1,
                    OpeningBalance = 0,
                    BalanceType = "Dr"
                };
                _context.Ledgers.Add(customerLedger);
                await _context.SaveChangesAsync();
            }

            var salesLedger = await _context.Ledgers.FirstOrDefaultAsync(l => l.LedgerName == "Sales Account");
            if (salesLedger == null)
            {
                salesLedger = new Ledger
                {
                    LedgerCode = "SALES-001",
                    LedgerName = "Sales Account",
                    AccountGroupId = (await _context.AccountGroups.FirstOrDefaultAsync(g => g.GroupName == "Sales Accounts"))?.Id ?? 1,
                    OpeningBalance = 0,
                    BalanceType = "Cr"
                };
                _context.Ledgers.Add(salesLedger);
                await _context.SaveChangesAsync();
            }

            var existingVoucher = await _context.Vouchers.Include(v => v.Items).FirstOrDefaultAsync(v => v.ReferenceNumber == invoice.InvoiceNumber);
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
                Narration = $"Sales invoice {invoice.InvoiceNumber} generated for customer {customer.CustomerName}",
                TotalAmount = invoice.GrandTotal
            };

            voucher.Items.Add(new VoucherItem
            {
                Id = 0,
                LedgerId = customerLedger.Id,
                DebitAmount = invoice.GrandTotal,
                CreditAmount = 0,
                Particulars = $"To Customer Sales Outstanding",
                SortOrder = 1
            });

            voucher.Items.Add(new VoucherItem
            {
                Id = 0,
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

    public async Task<(bool Success, string Message)> DeleteInvoiceAsync(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var item = await _context.SalesInvoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return (false, "Sales Invoice not found or already removed.");

            if (item.Status == "Paid")
                return (false, $"Sales Invoice '{item.InvoiceNumber}' cannot be deleted because it is already Paid.");

            item.IsActive = false;
            foreach (var line in item.Items)
            {
                var product = await _context.Products.FindAsync(line.ProductId);
                if (product != null)
                {
                    product.CurrentStock += line.Quantity;
                }
            }

            var stockTxns = await _context.StockTransactions
                .Where(t => t.TransactionType == "Sales" && t.ReferenceNumber == item.InvoiceNumber)
                .ToListAsync();
            _context.StockTransactions.RemoveRange(stockTxns);

            var voucher = await _context.Vouchers.Include(v => v.Items).FirstOrDefaultAsync(v => v.ReferenceNumber == item.InvoiceNumber);
            if (voucher != null)
            {
                _context.Vouchers.Remove(voucher);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, $"Sales Invoice '{item.InvoiceNumber}' deleted and stock reverted successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Failed to delete invoice: {ex.Message}");
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
        if (salesReturn.CustomerId <= 0)
            throw new InvalidOperationException("Customer selection is required.");
        if (salesReturn.Items == null || !salesReturn.Items.Any() || salesReturn.Items.Any(i => i.Quantity <= 0))
            throw new InvalidOperationException("Please add at least one line item with valid return quantity.");

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

    public async Task<(bool Success, string Message)> DeleteReturnAsync(int id)
    {
        var item = await _context.SalesReturns.FindAsync(id);
        if (item == null) return (false, "Sales Return not found or already removed.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Sales Return '{item.ReturnNumber}' deleted successfully.");
    }
}
