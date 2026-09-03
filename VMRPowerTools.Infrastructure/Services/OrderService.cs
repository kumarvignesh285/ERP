using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Domain.Entities;
using VMRPowerTools.Infrastructure.Data;

namespace VMRPowerTools.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly WebsiteDbContext _context;

    public OrderService(WebsiteDbContext context)
    {
        _context = context;
    }

    public async Task<SalesOrder> CheckoutAsync(CheckoutRequest request, CartSummary cart)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Find or Create Customer
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email != null && c.Email.ToLower() == request.Email.Trim().ToLower());

            if (customer == null)
            {
                var count = await _context.Customers.CountAsync() + 1;
                customer = new Customer
                {
                    CustomerCode = $"CUST-{DateTime.Today:yyyyMM}-{count:D3}",
                    CustomerName = request.Name,
                    Email = request.Email,
                    MobileNumber = request.Phone,
                    Address = request.Address,
                    City = request.City,
                    State = request.State,
                    Pincode = request.Pincode,
                    CreditLimit = 0,
                    OpeningBalance = 0,
                    BalanceType = "Dr"
                };
                await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync();
            }

            // 2. Generate Reference Numbers
            var orderIdSeed = Guid.NewGuid().ToString("N")[..6].ToUpper();
            var orderNumber = $"SO-{DateTime.Today:yyyyMMdd}-{orderIdSeed}";
            var invoiceNumber = $"INV-{DateTime.Today:yyyyMMdd}-{orderIdSeed}";

            // 3. Create Sales Order
            var salesOrder = new SalesOrder
            {
                OrderNumber = orderNumber,
                OrderDate = DateTime.Today,
                DeliveryDate = DateTime.Today.AddDays(3),
                CustomerId = customer.Id,
                Salesperson = "Web Portal",
                ReferenceNumber = request.Notes,
                SubTotal = cart.SubTotal,
                TaxAmount = cart.TaxAmount,
                DiscountAmount = cart.DiscountAmount,
                GrandTotal = cart.GrandTotal,
                Status = "Pending",
                ShippingAddress = $"{request.Address}, {request.City}, {request.State} - {request.Pincode}",
                WithGST = cart.TaxAmount > 0,
                CreatedAt = DateTime.Now,
                CreatedBy = request.Email
            };

            int sortIndex = 1;
            foreach (var cartItem in cart.Items)
            {
                salesOrder.Items.Add(new SalesOrderItem
                {
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.ProductName,
                    UnitName = "Pcs",
                    Quantity = cartItem.Quantity,
                    DeliveredQuantity = 0,
                    Rate = cartItem.Rate,
                    Discount = 0,
                    TaxPercentage = cartItem.TaxPercentage,
                    TaxAmount = cartItem.TaxAmount,
                    Amount = cartItem.TotalAmount,
                    SortOrder = sortIndex++
                });
            }

            await _context.SalesOrders.AddAsync(salesOrder);
            await _context.SaveChangesAsync(); // Fetch primary key ID for SalesOrder

            // 4. Create Sales Invoice
            var salesInvoice = new SalesInvoice
            {
                InvoiceNumber = invoiceNumber,
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(15),
                CustomerId = customer.Id,
                Salesperson = "Web Portal",
                ReferenceNumber = orderNumber,
                PaymentTerms = request.PaymentMethod,
                SubTotal = cart.SubTotal,
                TaxAmount = cart.TaxAmount,
                DiscountAmount = cart.DiscountAmount,
                RoundOff = 0,
                GrandTotal = cart.GrandTotal,
                PaidAmount = request.PaymentMethod == "Cash on Delivery" ? 0 : cart.GrandTotal,
                BalanceAmount = request.PaymentMethod == "Cash on Delivery" ? cart.GrandTotal : 0,
                Status = request.PaymentMethod == "Cash on Delivery" ? "Unpaid" : "Paid",
                SalesOrderId = salesOrder.Id,
                WithGST = cart.TaxAmount > 0,
                CreatedAt = DateTime.Now,
                CreatedBy = request.Email
            };

            sortIndex = 1;
            bool isLocalState = request.State.Trim().ToLower() == "tamil nadu";
            
            foreach (var item in cart.Items)
            {
                decimal itemDiscountAmount = cart.SubTotal > 0 ? (item.SubTotal / cart.SubTotal) * cart.DiscountAmount : 0;
                decimal itemSubtotalAfterDiscount = item.SubTotal - itemDiscountAmount;
                
                decimal cgst = isLocalState ? itemSubtotalAfterDiscount * (item.TaxPercentage / 200) : 0;
                decimal sgst = isLocalState ? itemSubtotalAfterDiscount * (item.TaxPercentage / 200) : 0;
                decimal igst = !isLocalState ? itemSubtotalAfterDiscount * (item.TaxPercentage / 100) : 0;

                salesInvoice.Items.Add(new SalesInvoiceItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    HSNCode = "8467", // Standard Power Tools HSN Code
                    UnitName = "Pcs",
                    Quantity = item.Quantity,
                    Rate = item.Rate,
                    Discount = 0,
                    DiscountAmount = itemDiscountAmount,
                    TaxPercentage = item.TaxPercentage,
                    CGSTAmount = cgst,
                    SGSTAmount = sgst,
                    IGSTAmount = igst,
                    TaxAmount = item.TaxAmount,
                    Amount = item.TotalAmount,
                    SortOrder = sortIndex++
                });

                // 5. Update Inventory and Create Stock Transaction
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.CurrentStock -= item.Quantity; // Reduce Stock

                    var stockTransaction = new StockTransaction
                    {
                        TransactionDate = DateTime.Now,
                        TransactionType = "Sales",
                        ReferenceNumber = invoiceNumber,
                        ProductId = product.Id,
                        WarehouseId = product.WarehouseId,
                        Quantity = -item.Quantity, // Negative value for stock reduction
                        Rate = item.Rate,
                        Remarks = $"Public website checkout Order #{orderNumber}",
                        CreatedAt = DateTime.Now,
                        CreatedBy = request.Email
                    };

                    await _context.StockTransactions.AddAsync(stockTransaction);
                }
            }

            await _context.SalesInvoices.AddAsync(salesInvoice);
            await _context.SaveChangesAsync();

            // 6. Generate ERP Accounting Voucher (Debits Customer, Credits Sales)
            var customerLedger = await _context.Ledgers
                .FirstOrDefaultAsync(l => l.LedgerName.Contains(customer.CustomerName) || l.LedgerCode == customer.CustomerCode);
            if (customerLedger == null)
            {
                var debtorGroup = await _context.AccountGroups.FirstOrDefaultAsync(g => g.GroupName == "Sundry Debtors");
                customerLedger = new Ledger
                {
                    LedgerCode = customer.CustomerCode,
                    LedgerName = customer.CustomerName,
                    AccountGroupId = debtorGroup?.Id ?? 1,
                    OpeningBalance = 0,
                    BalanceType = "Dr"
                };
                await _context.Ledgers.AddAsync(customerLedger);
                await _context.SaveChangesAsync();
            }

            var salesLedger = await _context.Ledgers
                .FirstOrDefaultAsync(l => l.LedgerName == "Sales Account" || l.LedgerCode == "SALES");
            if (salesLedger == null)
            {
                var salesGroup = await _context.AccountGroups.FirstOrDefaultAsync(g => g.GroupName == "Sales Accounts");
                salesLedger = new Ledger
                {
                    LedgerCode = "SALES",
                    LedgerName = "Sales Account",
                    AccountGroupId = salesGroup?.Id ?? 1,
                    OpeningBalance = 0,
                    BalanceType = "Cr"
                };
                await _context.Ledgers.AddAsync(salesLedger);
                await _context.SaveChangesAsync();
            }

            var voucher = new Voucher
            {
                VoucherNumber = "JV-" + salesInvoice.InvoiceNumber,
                VoucherDate = salesInvoice.InvoiceDate,
                VoucherType = "Journal",
                ReferenceNumber = salesInvoice.InvoiceNumber,
                Narration = $"Sales invoice {salesInvoice.InvoiceNumber} generated for customer {customer.CustomerName}",
                TotalAmount = salesInvoice.GrandTotal,
                CreatedAt = DateTime.Now,
                CreatedBy = request.Email
            };

            voucher.Items.Add(new VoucherItem
            {
                LedgerId = customerLedger.Id,
                DebitAmount = salesInvoice.GrandTotal,
                CreditAmount = 0,
                Particulars = "To Customer Sales Outstanding",
                SortOrder = 1
            });

            voucher.Items.Add(new VoucherItem
            {
                LedgerId = salesLedger.Id,
                DebitAmount = 0,
                CreditAmount = salesInvoice.GrandTotal,
                Particulars = "By Credit to Sales",
                SortOrder = 2
            });

            await _context.Vouchers.AddAsync(voucher);
            await _context.SaveChangesAsync();

            await dbTransaction.CommitAsync();
            return salesOrder;
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<SalesOrder>> GetOrderHistoryAsync(string email)
    {
        return await _context.SalesOrders
            .Include(o => o.Items)
            .Where(o => o.CreatedBy == email)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<SalesOrder?> GetOrderDetailsAsync(int orderId)
    {
        return await _context.SalesOrders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }
}
