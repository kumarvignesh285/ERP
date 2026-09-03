using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;
using System.Globalization;
using MiniExcelLibs;

namespace ERP.Services;

public class MasterService : IMasterService
{
    private readonly AppDbContext _context;
    private readonly IPdfProductParserService _pdfParserService;
    private readonly ICompanyContext _companyContext;
    private readonly IAuditService _auditService;

    public MasterService(
        AppDbContext context,
        IPdfProductParserService pdfParserService,
        ICompanyContext companyContext,
        IAuditService auditService)
    {
        _context = context;
        _pdfParserService = pdfParserService;
        _companyContext = companyContext;
        _auditService = auditService;
    }

    private async Task UpdateEntityAsync<T>(T entity) where T : BaseEntity
    {
        var existing = await _context.Set<T>().FirstOrDefaultAsync(e => e.Id == entity.Id);
        if (existing == null)
        {
            throw new UnauthorizedAccessException("Record not found or access denied for the current company context.");
        }

        entity.CreatedAt = existing.CreatedAt;
        entity.CreatedBy = existing.CreatedBy;
        entity.IsActive = existing.IsActive;

        if (entity is ICompanyOwned companyOwned && existing is ICompanyOwned existingCompanyOwned)
        {
            companyOwned.CompanyId = existingCompanyOwned.CompanyId;
        }

        if (entity is Product prod && existing is Product existingProd)
        {
            prod.OpeningStock = existingProd.OpeningStock;
            prod.CurrentStock = existingProd.CurrentStock;
        }
        else if (entity is Customer cust && existing is Customer existingCust)
        {
            cust.OpeningBalance = existingCust.OpeningBalance;
            cust.BalanceType = existingCust.BalanceType;
        }
        else if (entity is Supplier supp && existing is Supplier existingSupp)
        {
            supp.OpeningBalance = existingSupp.OpeningBalance;
            supp.BalanceType = existingSupp.BalanceType;
        }

        _context.Entry(existing).CurrentValues.SetValues(entity);
    }

    // Company
    public async Task<Company?> GetCompanyAsync()
    {
        if (_companyContext.CurrentCompanyId.HasValue && _companyContext.CurrentCompanyId.Value > 0)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == _companyContext.CurrentCompanyId.Value);
            if (company != null) return company;
        }
        return await _context.Companies.FirstOrDefaultAsync(c => c.IsActive);
    }

    public async Task<Company> SaveCompanyAsync(Company company)
    {
        if (company.Id == 0)
        {
            _context.Companies.Add(company);
        }
        else
        {
            await UpdateEntityAsync(company);
        }
        await _context.SaveChangesAsync();
        return company;
    }

    public async Task<Company> SaveCompanyWithLogoAsync(Company company, IFormFile? logoFile, IWebHostEnvironment env)
    {
        if (logoFile != null && logoFile.Length > 0)
        {
            var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "company");
            Directory.CreateDirectory(uploadsDir);

            var extension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
            if (extension is not (".png" or ".jpg" or ".jpeg" or ".gif" or ".webp"))
            {
                throw new InvalidOperationException("Logo must be PNG, JPG, GIF, or WEBP.");
            }

            if (logoFile.Length > 2 * 1024 * 1024)
            {
                throw new InvalidOperationException("Logo file size must be under 2 MB.");
            }

            var fileName = $"logo_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            company.Logo = $"/uploads/company/{fileName}";
        }

        return await SaveCompanyAsync(company);
    }

    public async Task<List<Company>> GetAllCompaniesAsync(string? search = null, string? status = null)
    {
        var query = _context.Companies.IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(c =>
                c.CompanyCode.Contains(s) ||
                c.CompanyName.Contains(s) ||
                (c.Phone != null && c.Phone.Contains(s)) ||
                (c.Email != null && c.Email.Contains(s)) ||
                (c.GSTNumber != null && c.GSTNumber.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.IsActive);
            }
            else if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => !c.IsActive);
            }
        }

        return await query.OrderBy(c => c.Id).ToListAsync();
    }

    public async Task<Company?> GetCompanyByIdAsync(int id)
    {
        return await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> IsCompanyCodeAvailableAsync(string companyCode, int? excludeCompanyId = null)
    {
        if (string.IsNullOrWhiteSpace(companyCode))
            return false;

        var normalized = companyCode.Trim().ToUpperInvariant();

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[A-Z0-9\-_]{2,20}$"))
            return false;

        var query = _context.Companies.IgnoreQueryFilters().Where(c => c.CompanyCode == normalized);
        if (excludeCompanyId.HasValue && excludeCompanyId.Value > 0)
        {
            query = query.Where(c => c.Id != excludeCompanyId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<(bool Success, string? ErrorMessage, Company? Company)> CreateCompanyAsync(Company company, IFormFile? logoFile, IWebHostEnvironment env, string? currentUserId)
    {
        if (string.IsNullOrWhiteSpace(company.CompanyName))
            return (false, "Company Name is required.", null);

        if (string.IsNullOrWhiteSpace(company.CompanyCode))
            return (false, "Company Code is required.", null);

        var normalizedCode = company.CompanyCode.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedCode, @"^[A-Z0-9\-_]{2,20}$"))
            return (false, "Company Code must be between 2 and 20 characters and contain only letters, numbers, and hyphens.", null);

        var isAvailable = await IsCompanyCodeAvailableAsync(normalizedCode);
        if (!isAvailable)
            return (false, "Company Code already exists. Please use a different Company Code.", null);

        company.CompanyCode = normalizedCode;
        company.CompanyName = company.CompanyName.Trim();
        company.Currency = string.IsNullOrWhiteSpace(company.Currency) ? "INR" : company.Currency.Trim();
        company.FinancialYear = company.FinancialYear?.Trim();
        company.BusinessType = company.BusinessType?.Trim();
        company.Phone = company.Phone?.Trim();
        company.AlternatePhone = company.AlternatePhone?.Trim();
        company.Email = company.Email?.Trim();
        company.GSTNumber = company.GSTNumber?.Trim().ToUpperInvariant();
        company.PANNumber = company.PANNumber?.Trim().ToUpperInvariant();
        company.CreatedAt = DateTime.Now;
        company.CreatedBy = currentUserId ?? "Super Admin";

        if (logoFile != null && logoFile.Length > 0)
        {
            var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "company");
            Directory.CreateDirectory(uploadsDir);

            var extension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
            if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            {
                return (false, "Logo must be PNG, JPG, JPEG, or WEBP.", null);
            }

            if (logoFile.Length > 2 * 1024 * 1024)
            {
                return (false, "Logo file size must be under 2 MB.", null);
            }

            var fileName = $"logo_{normalizedCode}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            company.Logo = $"/uploads/company/{fileName}";
        }

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        return (true, null, company);
    }

    public async Task<(bool Success, string? ErrorMessage, Company? Company)> UpdateCompanyAsync(Company company, IFormFile? logoFile, IWebHostEnvironment env, string? currentUserId)
    {
        var existing = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == company.Id);
        if (existing == null)
            return (false, "Company not found.", null);

        if (string.IsNullOrWhiteSpace(company.CompanyName))
            return (false, "Company Name is required.", null);

        // Keep CompanyCode locked to existing value
        existing.CompanyName = company.CompanyName.Trim();
        existing.BusinessType = company.BusinessType?.Trim();
        existing.Address = company.Address?.Trim();
        existing.City = company.City?.Trim();
        existing.State = company.State?.Trim();
        existing.Country = string.IsNullOrWhiteSpace(company.Country) ? "India" : company.Country.Trim();
        existing.Pincode = company.Pincode?.Trim();
        existing.Phone = company.Phone?.Trim();
        existing.AlternatePhone = company.AlternatePhone?.Trim();
        existing.Email = company.Email?.Trim();
        existing.Website = company.Website?.Trim();
        existing.GSTNumber = company.GSTNumber?.Trim().ToUpperInvariant();
        existing.PANNumber = company.PANNumber?.Trim().ToUpperInvariant();
        existing.Currency = string.IsNullOrWhiteSpace(company.Currency) ? "INR" : company.Currency.Trim();
        existing.FinancialYear = company.FinancialYear?.Trim();
        existing.IsActive = company.IsActive;
        existing.UpdatedAt = DateTime.Now;
        existing.UpdatedBy = currentUserId ?? "Super Admin";

        if (logoFile != null && logoFile.Length > 0)
        {
            var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "company");
            Directory.CreateDirectory(uploadsDir);

            var extension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
            if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            {
                return (false, "Logo must be PNG, JPG, JPEG, or WEBP.", null);
            }

            if (logoFile.Length > 2 * 1024 * 1024)
            {
                return (false, "Logo file size must be under 2 MB.", null);
            }

            var fileName = $"logo_{existing.CompanyCode}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            existing.Logo = $"/uploads/company/{fileName}";
        }

        await _context.SaveChangesAsync();
        return (true, null, existing);
    }

    public async Task<(bool Success, string? ErrorMessage, bool NewStatus)> ToggleCompanyStatusAsync(int id, string? currentUserId)
    {
        var existing = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (existing == null)
            return (false, "Company not found.", false);

        existing.IsActive = !existing.IsActive;
        existing.UpdatedAt = DateTime.Now;
        existing.UpdatedBy = currentUserId ?? "Super Admin";

        await _context.SaveChangesAsync();
        return (true, null, existing.IsActive);
    }

    public async Task<string> GetNextBillNumberPreviewAsync(string billType)
    {
        var company = await GetCompanyAsync() ?? new Company();
        return FormatBillNumber(company, billType, billType.Equals("purchase", StringComparison.OrdinalIgnoreCase)
            ? company.PurchaseBillNextNumber
            : company.SalesBillNextNumber);
    }

    public async Task<string> ReserveNextBillNumberAsync(string billType)
    {
        var company = await GetCompanyAsync()
            ?? throw new InvalidOperationException("Company settings not found.");

        var isPurchase = billType.Equals("purchase", StringComparison.OrdinalIgnoreCase);
        var number = isPurchase ? company.PurchaseBillNextNumber : company.SalesBillNextNumber;
        var billNumber = FormatBillNumber(company, billType, number);

        if (isPurchase)
        {
            company.PurchaseBillNextNumber = number + 1;
        }
        else
        {
            company.SalesBillNextNumber = number + 1;
        }

        company.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return billNumber;
    }

    private static string FormatBillNumber(Company company, string billType, int number)
    {
        var prefix = billType.Equals("purchase", StringComparison.OrdinalIgnoreCase)
            ? company.PurchaseBillPrefix
            : company.SalesBillPrefix;
        return $"{prefix}{number:D6}";
    }

    // Customer
    public async Task<List<Customer>> GetCustomersAsync()
    {
        return await _context.Customers.Where(c => c.IsActive).ToListAsync();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer> SaveCustomerAsync(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            throw new InvalidOperationException("Customer Code is required.");
        if (string.IsNullOrWhiteSpace(customer.CustomerName))
            throw new InvalidOperationException("Customer Name is required.");

        customer.CustomerCode = customer.CustomerCode.Trim();
        customer.CustomerName = customer.CustomerName.Trim();

        var duplicateCode = await _context.Customers
            .AnyAsync(c => c.CustomerCode.ToLower() == customer.CustomerCode.ToLower() && c.Id != customer.Id && c.IsActive);
        if (duplicateCode)
            throw new InvalidOperationException($"Customer Code '{customer.CustomerCode}' already exists. Please enter a different code.");

        Customer? previous = null;
        var isNew = customer.Id == 0;

        if (isNew)
        {
            _context.Customers.Add(customer);
        }
        else
        {
            var existing = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customer.Id);
            if (existing != null)
            {
                previous = new Customer
                {
                    CustomerName = existing.CustomerName,
                    CustomerCode = existing.CustomerCode,
                    MobileNumber = existing.MobileNumber,
                    Email = existing.Email,
                    CreditLimit = existing.CreditLimit,
                    CreditDays = existing.CreditDays
                };
            }
            await UpdateEntityAsync(customer);
        }
        await _context.SaveChangesAsync();

        await _auditService.LogCrudAsync(
            action: isNew ? "CREATE" : "UPDATE",
            module: "Customers",
            entityName: "Customer",
            entityId: customer.Id.ToString(),
            description: isNew ? $"Created customer '{customer.CustomerName}' ({customer.CustomerCode})" : $"Updated customer '{customer.CustomerName}'",
            oldValues: previous,
            newValues: new { customer.CustomerName, customer.CustomerCode, customer.MobileNumber, customer.Email, customer.CreditLimit, customer.CreditDays },
            companyId: customer.CompanyId);

        return customer;
    }

    public async Task<(bool Success, string Message)> DeleteCustomerAsync(int id)
    {
        var item = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (item == null) return (false, "Customer not found or already removed.");

        var invoiceCount = await _context.SalesInvoices.CountAsync(i => i.CustomerId == id && i.IsActive);
        if (invoiceCount > 0)
            return (false, $"Customer '{item.CustomerName}' cannot be deleted because {invoiceCount} Sales Invoice(s) are linked to this customer.");

        var orderCount = await _context.SalesOrders.CountAsync(o => o.CustomerId == id && o.IsActive);
        if (orderCount > 0)
            return (false, $"Customer '{item.CustomerName}' cannot be deleted because {orderCount} Sales Order(s) are linked to this customer.");

        var challanCount = await _context.DeliveryChallans.CountAsync(c => c.CustomerId == id && c.IsActive);
        if (challanCount > 0)
            return (false, $"Customer '{item.CustomerName}' cannot be deleted because {challanCount} Delivery Challan(s) are linked to this customer.");

        var quoteCount = await _context.SalesQuotations.CountAsync(q => q.CustomerId == id && q.IsActive);
        if (quoteCount > 0)
            return (false, $"Customer '{item.CustomerName}' cannot be deleted because {quoteCount} Sales Quotation(s) are linked to this customer.");

        var returnCount = await _context.SalesReturns.CountAsync(r => r.CustomerId == id && r.IsActive);
        if (returnCount > 0)
            return (false, $"Customer '{item.CustomerName}' cannot be deleted because {returnCount} Sales Return(s) are linked to this customer.");

        item.IsActive = false;
        await _context.SaveChangesAsync();

        await _auditService.LogCrudAsync(
            action: "DELETE",
            module: "Customers",
            entityName: "Customer",
            entityId: id.ToString(),
            description: $"Deactivated customer '{item.CustomerName}' ({item.CustomerCode})",
            oldValues: new { item.CustomerName, item.CustomerCode, item.IsActive },
            companyId: item.CompanyId);

        return (true, $"Customer '{item.CustomerName}' deleted successfully.");
    }

    // Supplier
    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        return await _context.Suppliers.Where(s => s.IsActive).ToListAsync();
    }

    public async Task<Supplier?> GetSupplierByIdAsync(int id)
    {
        return await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Supplier> SaveSupplierAsync(Supplier supplier)
    {
        if (string.IsNullOrWhiteSpace(supplier.SupplierCode))
            throw new InvalidOperationException("Supplier Code is required.");
        if (string.IsNullOrWhiteSpace(supplier.SupplierName))
            throw new InvalidOperationException("Supplier Name is required.");

        supplier.SupplierCode = supplier.SupplierCode.Trim();
        supplier.SupplierName = supplier.SupplierName.Trim();

        var duplicateCode = await _context.Suppliers
            .AnyAsync(s => s.SupplierCode.ToLower() == supplier.SupplierCode.ToLower() && s.Id != supplier.Id && s.IsActive);
        if (duplicateCode)
            throw new InvalidOperationException($"Supplier Code '{supplier.SupplierCode}' already exists. Please enter a different code.");

        Supplier? previous = null;
        var isNew = supplier.Id == 0;

        if (isNew)
        {
            _context.Suppliers.Add(supplier);
        }
        else
        {
            var existing = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == supplier.Id);
            if (existing != null)
            {
                previous = new Supplier
                {
                    SupplierName = existing.SupplierName,
                    SupplierCode = existing.SupplierCode,
                    Mobile = existing.Mobile,
                    Email = existing.Email,
                    CreditLimit = existing.CreditLimit,
                    CreditDays = existing.CreditDays
                };
            }
            await UpdateEntityAsync(supplier);
        }
        await _context.SaveChangesAsync();

        await _auditService.LogCrudAsync(
            action: isNew ? "CREATE" : "UPDATE",
            module: "Suppliers",
            entityName: "Supplier",
            entityId: supplier.Id.ToString(),
            description: isNew ? $"Created supplier '{supplier.SupplierName}' ({supplier.SupplierCode})" : $"Updated supplier '{supplier.SupplierName}'",
            oldValues: previous,
            newValues: new { supplier.SupplierName, supplier.SupplierCode, supplier.Mobile, supplier.Email, supplier.CreditLimit, supplier.CreditDays },
            companyId: supplier.CompanyId);

        return supplier;
    }

    public async Task<(bool Success, string Message)> DeleteSupplierAsync(int id)
    {
        var item = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        if (item == null) return (false, "Supplier not found or already removed.");

        var invoiceCount = await _context.PurchaseInvoices.CountAsync(i => i.SupplierId == id && i.IsActive);
        if (invoiceCount > 0)
            return (false, $"Supplier '{item.SupplierName}' cannot be deleted because {invoiceCount} Purchase Invoice(s) are linked to this supplier.");

        var orderCount = await _context.PurchaseOrders.CountAsync(o => o.SupplierId == id && o.IsActive);
        if (orderCount > 0)
            return (false, $"Supplier '{item.SupplierName}' cannot be deleted because {orderCount} Purchase Order(s) are linked to this supplier.");

        var grnCount = await _context.GoodsReceiptNotes.CountAsync(g => g.SupplierId == id && g.IsActive);
        if (grnCount > 0)
            return (false, $"Supplier '{item.SupplierName}' cannot be deleted because {grnCount} Goods Receipt Note(s) are linked to this supplier.");

        var returnCount = await _context.PurchaseReturns.CountAsync(r => r.SupplierId == id && r.IsActive);
        if (returnCount > 0)
            return (false, $"Supplier '{item.SupplierName}' cannot be deleted because {returnCount} Purchase Return(s) are linked to this supplier.");

        item.IsActive = false;
        await _context.SaveChangesAsync();

        await _auditService.LogCrudAsync(
            action: "DELETE",
            module: "Suppliers",
            entityName: "Supplier",
            entityId: id.ToString(),
            description: $"Deactivated supplier '{item.SupplierName}' ({item.SupplierCode})",
            oldValues: new { item.SupplierName, item.SupplierCode, item.IsActive },
            companyId: item.CompanyId);

        return (true, $"Supplier '{item.SupplierName}' deleted successfully.");
    }

    // Product
    public async Task<List<Product>> GetProductsAsync()
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.Warehouse)
            .Where(p => p.IsActive).ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.Warehouse)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product> SaveProductAsync(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.ProductCode))
            throw new InvalidOperationException("Product Code is required.");
        if (string.IsNullOrWhiteSpace(product.ProductName))
            throw new InvalidOperationException("Product Name is required.");

        product.ProductCode = product.ProductCode.Trim();
        product.ProductName = product.ProductName.Trim();

        var duplicateCode = await _context.Products
            .AnyAsync(p => p.ProductCode.ToLower() == product.ProductCode.ToLower() && p.Id != product.Id && p.IsActive);
        if (duplicateCode)
            throw new InvalidOperationException($"Product Code '{product.ProductCode}' already belongs to another product. Please use a unique product code.");

        Product? previous = null;
        var isNew = product.Id == 0;

        if (isNew)
        {
            _context.Products.Add(product);
        }
        else
        {
            var existing = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
            if (existing != null)
            {
                previous = new Product
                {
                    ProductName = existing.ProductName,
                    ProductCode = existing.ProductCode,
                    PurchasePrice = existing.PurchasePrice,
                    SalesPrice = existing.SalesPrice,
                    MRP = existing.MRP,
                    GSTPercentage = existing.GSTPercentage
                };
            }
            await UpdateEntityAsync(product);
        }
        await _context.SaveChangesAsync();

        await _auditService.LogCrudAsync(
            action: isNew ? "CREATE" : "UPDATE",
            module: "Products",
            entityName: "Product",
            entityId: product.Id.ToString(),
            description: isNew ? $"Created product '{product.ProductName}' ({product.ProductCode})" : $"Updated product '{product.ProductName}'",
            oldValues: previous != null ? new { previous.ProductName, previous.ProductCode, previous.PurchasePrice, previous.SalesPrice, previous.MRP, previous.GSTPercentage } : null,
            newValues: new { product.ProductName, product.ProductCode, product.PurchasePrice, product.SalesPrice, product.MRP, product.GSTPercentage },
            companyId: product.CompanyId);

        return product;
    }

    public async Task<(bool Success, string Message)> DeleteProductAsync(int id)
    {
        var item = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (item == null) return (false, "Product not found or already removed.");

        var inSales = await _context.SalesInvoiceItems.AnyAsync(i => i.ProductId == id);
        if (inSales)
            return (false, $"Product '{item.ProductName}' cannot be deleted because it is used in existing Sales Invoices.");

        var inPurchases = await _context.PurchaseInvoiceItems.AnyAsync(i => i.ProductId == id);
        if (inPurchases)
            return (false, $"Product '{item.ProductName}' cannot be deleted because it is used in existing Purchase Invoices.");

        var inOrders = await _context.SalesOrderItems.AnyAsync(i => i.ProductId == id) || await _context.PurchaseOrderItems.AnyAsync(i => i.ProductId == id);
        if (inOrders)
            return (false, $"Product '{item.ProductName}' cannot be deleted because it is referenced in Orders.");

        var inStock = await _context.StockTransactions.AnyAsync(t => t.ProductId == id);
        if (inStock && item.CurrentStock != 0)
            return (false, $"Product '{item.ProductName}' cannot be deleted because it currently has active stock ({item.CurrentStock}) and transaction history.");

        item.IsActive = false;
        await _context.SaveChangesAsync();

        await _auditService.LogCrudAsync(
            action: "DELETE",
            module: "Products",
            entityName: "Product",
            entityId: id.ToString(),
            description: $"Deactivated product '{item.ProductName}' ({item.ProductCode})",
            oldValues: new { item.ProductName, item.ProductCode, item.IsActive },
            companyId: item.CompanyId);

        return (true, $"Product '{item.ProductName}' deleted successfully.");
    }

    public async Task ClearAllProductDataAsync()
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.SalesQuotationItems.RemoveRange(_context.SalesQuotationItems);
            _context.SalesOrderItems.RemoveRange(_context.SalesOrderItems);
            _context.DeliveryChallanItems.RemoveRange(_context.DeliveryChallanItems);
            _context.SalesInvoiceItems.RemoveRange(_context.SalesInvoiceItems);
            _context.SalesReturnItems.RemoveRange(_context.SalesReturnItems);

            _context.PurchaseOrderItems.RemoveRange(_context.PurchaseOrderItems);
            _context.GoodsReceiptNoteItems.RemoveRange(_context.GoodsReceiptNoteItems);
            _context.PurchaseInvoiceItems.RemoveRange(_context.PurchaseInvoiceItems);
            _context.PurchaseReturnItems.RemoveRange(_context.PurchaseReturnItems);

            _context.StockTransferItems.RemoveRange(_context.StockTransferItems);
            _context.StockAdjustmentItems.RemoveRange(_context.StockAdjustmentItems);
            _context.PhysicalStockVerificationItems.RemoveRange(_context.PhysicalStockVerificationItems);
            _context.StockTransactions.RemoveRange(_context.StockTransactions);

            await _context.SaveChangesAsync();

            _context.Products.RemoveRange(_context.Products);
            await _context.SaveChangesAsync();

            _context.Categories.RemoveRange(_context.Categories);
            _context.Brands.RemoveRange(_context.Brands);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Category
    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await _context.Categories.Where(c => c.IsActive).ToListAsync();
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Category> SaveCategoryAsync(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.CategoryName))
            throw new InvalidOperationException("Category Name is required.");

        category.CategoryName = category.CategoryName.Trim();

        var duplicate = await _context.Categories
            .AnyAsync(c => c.CategoryName.ToLower() == category.CategoryName.ToLower() && c.Id != category.Id && c.IsActive);
        if (duplicate)
            throw new InvalidOperationException($"Category '{category.CategoryName}' already exists. Please choose a different category name.");

        if (category.Id == 0)
        {
            _context.Categories.Add(category);
        }
        else
        {
            await UpdateEntityAsync(category);
        }
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
    {
        var item = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (item == null) return (false, "Category not found or already removed.");

        var productCount = await _context.Products.CountAsync(p => p.CategoryId == id && p.IsActive);
        if (productCount > 0)
            return (false, $"Category '{item.CategoryName}' cannot be deleted because {productCount} active product(s) belong to this category.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Category '{item.CategoryName}' deleted successfully.");
    }

    // Brand
    public async Task<List<Brand>> GetBrandsAsync()
    {
        return await _context.Brands.Where(b => b.IsActive).ToListAsync();
    }

    public async Task<Brand?> GetBrandByIdAsync(int id)
    {
        return await _context.Brands.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Brand> SaveBrandAsync(Brand brand)
    {
        if (string.IsNullOrWhiteSpace(brand.BrandName))
            throw new InvalidOperationException("Brand Name is required.");

        brand.BrandName = brand.BrandName.Trim();

        var duplicate = await _context.Brands
            .AnyAsync(b => b.BrandName.ToLower() == brand.BrandName.ToLower() && b.Id != brand.Id && b.IsActive);
        if (duplicate)
            throw new InvalidOperationException($"Brand '{brand.BrandName}' already exists. Please choose a different brand name.");

        if (brand.Id == 0)
        {
            _context.Brands.Add(brand);
        }
        else
        {
            await UpdateEntityAsync(brand);
        }
        await _context.SaveChangesAsync();
        return brand;
    }

    public async Task<(bool Success, string Message)> DeleteBrandAsync(int id)
    {
        var item = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id);
        if (item == null) return (false, "Brand not found or already removed.");

        var productCount = await _context.Products.CountAsync(p => p.BrandId == id && p.IsActive);
        if (productCount > 0)
            return (false, $"Brand '{item.BrandName}' cannot be deleted because {productCount} active product(s) belong to this brand.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Brand '{item.BrandName}' deleted successfully.");
    }

    // Unit
    public async Task<List<Unit>> GetUnitsAsync()
    {
        return await _context.Units.Where(u => u.IsActive).ToListAsync();
    }

    public async Task<Unit?> GetUnitByIdAsync(int id)
    {
        return await _context.Units.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Unit> SaveUnitAsync(Unit unit)
    {
        if (string.IsNullOrWhiteSpace(unit.UnitName))
            throw new InvalidOperationException("Unit Name is required.");
        if (string.IsNullOrWhiteSpace(unit.UnitSymbol))
            throw new InvalidOperationException("Unit Symbol is required.");

        unit.UnitName = unit.UnitName.Trim();
        unit.UnitSymbol = unit.UnitSymbol.Trim();

        var duplicate = await _context.Units
            .AnyAsync(u => (u.UnitSymbol.ToLower() == unit.UnitSymbol.ToLower() || u.UnitName.ToLower() == unit.UnitName.ToLower()) && u.Id != unit.Id && u.IsActive);
        if (duplicate)
            throw new InvalidOperationException($"Unit with symbol '{unit.UnitSymbol}' or name '{unit.UnitName}' already exists.");

        if (unit.Id == 0)
        {
            _context.Units.Add(unit);
        }
        else
        {
            await UpdateEntityAsync(unit);
        }
        await _context.SaveChangesAsync();
        return unit;
    }

    public async Task<(bool Success, string Message)> DeleteUnitAsync(int id)
    {
        var item = await _context.Units.FirstOrDefaultAsync(u => u.Id == id);
        if (item == null) return (false, "Unit not found or already removed.");

        var productCount = await _context.Products.CountAsync(p => p.UnitId == id && p.IsActive);
        if (productCount > 0)
            return (false, $"Unit '{item.UnitSymbol}' cannot be deleted because {productCount} active product(s) use this measurement unit.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Unit '{item.UnitSymbol}' deleted successfully.");
    }

    // Warehouse
    public async Task<List<Warehouse>> GetWarehousesAsync()
    {
        return await _context.Warehouses.Where(w => w.IsActive).ToListAsync();
    }

    public async Task<Warehouse?> GetWarehouseByIdAsync(int id)
    {
        return await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<Warehouse> SaveWarehouseAsync(Warehouse warehouse)
    {
        if (string.IsNullOrWhiteSpace(warehouse.WarehouseName))
            throw new InvalidOperationException("Warehouse Name is required.");

        warehouse.WarehouseName = warehouse.WarehouseName.Trim();

        var duplicate = await _context.Warehouses
            .AnyAsync(w => w.WarehouseName.ToLower() == warehouse.WarehouseName.ToLower() && w.Id != warehouse.Id && w.IsActive);
        if (duplicate)
            throw new InvalidOperationException($"Warehouse '{warehouse.WarehouseName}' already exists. Please choose a different warehouse name.");

        if (warehouse.Id == 0)
        {
            _context.Warehouses.Add(warehouse);
        }
        else
        {
            await UpdateEntityAsync(warehouse);
        }
        await _context.SaveChangesAsync();
        return warehouse;
    }

    public async Task<(bool Success, string Message)> DeleteWarehouseAsync(int id)
    {
        var item = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id);
        if (item == null) return (false, "Warehouse not found or already removed.");

        var productCount = await _context.Products.CountAsync(p => p.WarehouseId == id && p.IsActive);
        if (productCount > 0)
            return (false, $"Warehouse '{item.WarehouseName}' cannot be deleted because {productCount} product(s) are stored in it.");

        var transferCount = await _context.StockTransfers.CountAsync(t => (t.FromWarehouseId == id || t.ToWarehouseId == id) && t.IsActive);
        if (transferCount > 0)
            return (false, $"Warehouse '{item.WarehouseName}' cannot be deleted because {transferCount} stock transfer record(s) reference it.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Warehouse '{item.WarehouseName}' deleted successfully.");
    }

    // Employee
    public async Task<List<Employee>> GetEmployeesAsync()
    {
        return await _context.Employees.Where(e => e.IsActive).ToListAsync();
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        return await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Employee> SaveEmployeeAsync(Employee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.EmployeeCode))
            throw new InvalidOperationException("Employee Code is required.");
        if (string.IsNullOrWhiteSpace(employee.EmployeeName))
            throw new InvalidOperationException("Employee Name is required.");

        employee.EmployeeCode = employee.EmployeeCode.Trim();
        employee.EmployeeName = employee.EmployeeName.Trim();

        var duplicate = await _context.Employees
            .AnyAsync(e => e.EmployeeCode.ToLower() == employee.EmployeeCode.ToLower() && e.Id != employee.Id && e.IsActive);
        if (duplicate)
            throw new InvalidOperationException($"Employee Code '{employee.EmployeeCode}' already exists for another employee.");

        if (employee.Id == 0)
        {
            _context.Employees.Add(employee);
        }
        else
        {
            await UpdateEntityAsync(employee);
        }
        await _context.SaveChangesAsync();
        return employee;
    }

    public async Task<(bool Success, string Message)> DeleteEmployeeAsync(int id)
    {
        var item = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (item == null) return (false, "Employee not found or already removed.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Employee '{item.EmployeeName}' deleted successfully.");
    }

    // Account Group
    public async Task<List<AccountGroup>> GetAccountGroupsAsync()
    {
        return await _context.AccountGroups.Where(ag => ag.IsActive).ToListAsync();
    }

    public async Task<AccountGroup?> GetAccountGroupByIdAsync(int id)
    {
        return await _context.AccountGroups.FirstOrDefaultAsync(ag => ag.Id == id);
    }

    public async Task<AccountGroup> SaveAccountGroupAsync(AccountGroup group)
    {
        if (string.IsNullOrWhiteSpace(group.GroupName))
            throw new InvalidOperationException("Group Name is required.");

        group.GroupName = group.GroupName.Trim();

        var duplicate = await _context.AccountGroups
            .AnyAsync(ag => ag.GroupName.ToLower() == group.GroupName.ToLower() && ag.Id != group.Id && ag.IsActive);
        if (duplicate)
            throw new InvalidOperationException($"Account Group '{group.GroupName}' already exists.");

        if (group.Id == 0)
        {
            _context.AccountGroups.Add(group);
        }
        else
        {
            await UpdateEntityAsync(group);
        }
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<(bool Success, string Message)> DeleteAccountGroupAsync(int id)
    {
        var item = await _context.AccountGroups.FirstOrDefaultAsync(ag => ag.Id == id);
        if (item == null) return (false, "Account Group not found or already removed.");

        var ledgerCount = await _context.Ledgers.CountAsync(l => l.AccountGroupId == id && l.IsActive);
        if (ledgerCount > 0)
            return (false, $"Account Group '{item.GroupName}' cannot be deleted because {ledgerCount} ledger(s) are grouped under it.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Account Group '{item.GroupName}' deleted successfully.");
    }

    // Ledger
    public async Task<List<Ledger>> GetLedgersAsync()
    {
        return await _context.Ledgers.Include(l => l.AccountGroup).Where(l => l.IsActive).ToListAsync();
    }

    public async Task<Ledger?> GetLedgerByIdAsync(int id)
    {
        return await _context.Ledgers.Include(l => l.AccountGroup).FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<Ledger> SaveLedgerAsync(Ledger ledger)
    {
        if (string.IsNullOrWhiteSpace(ledger.LedgerCode))
            throw new InvalidOperationException("Ledger Code is required.");
        if (string.IsNullOrWhiteSpace(ledger.LedgerName))
            throw new InvalidOperationException("Ledger Name is required.");

        ledger.LedgerCode = ledger.LedgerCode.Trim();
        ledger.LedgerName = ledger.LedgerName.Trim();

        var duplicate = await _context.Ledgers
            .AnyAsync(l => (l.LedgerCode.ToLower() == ledger.LedgerCode.ToLower() || l.LedgerName.ToLower() == ledger.LedgerName.ToLower()) && l.Id != ledger.Id && l.IsActive);
        if (duplicate)
            throw new InvalidOperationException($"Ledger with code '{ledger.LedgerCode}' or name '{ledger.LedgerName}' already exists.");

        if (ledger.Id == 0)
        {
            _context.Ledgers.Add(ledger);
        }
        else
        {
            await UpdateEntityAsync(ledger);
        }
        await _context.SaveChangesAsync();
        return ledger;
    }

    public async Task<(bool Success, string Message)> DeleteLedgerAsync(int id)
    {
        var item = await _context.Ledgers.FirstOrDefaultAsync(l => l.Id == id);
        if (item == null) return (false, "Ledger not found or already removed.");

        var voucherCount = await _context.VoucherItems.CountAsync(v => v.LedgerId == id);
        if (voucherCount > 0)
            return (false, $"Ledger '{item.LedgerName}' cannot be deleted because {voucherCount} voucher transaction(s) exist for this account.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Ledger '{item.LedgerName}' deleted successfully.");
    }

    // Bank
    public async Task<List<Bank>> GetBanksAsync()
    {
        return await _context.Banks.Include(b => b.Ledger).Where(b => b.IsActive).ToListAsync();
    }

    public async Task<Bank?> GetBankByIdAsync(int id)
    {
        return await _context.Banks.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Bank> SaveBankAsync(Bank bank)
    {
        if (string.IsNullOrWhiteSpace(bank.BankName))
            throw new InvalidOperationException("Bank Name is required.");
        if (string.IsNullOrWhiteSpace(bank.AccountNumber))
            throw new InvalidOperationException("Account Number is required.");

        bank.BankName = bank.BankName.Trim();
        bank.AccountNumber = bank.AccountNumber.Trim();

        var duplicate = await _context.Banks
            .AnyAsync(b => b.AccountNumber != null && b.AccountNumber.ToLower() == bank.AccountNumber.ToLower() && b.Id != bank.Id && b.IsActive);
        if (duplicate)
            throw new InvalidOperationException($"Bank Account Number '{bank.AccountNumber}' already exists.");

        if (bank.Id == 0)
        {
            _context.Banks.Add(bank);
        }
        else
        {
            await UpdateEntityAsync(bank);
        }
        await _context.SaveChangesAsync();
        return bank;
    }

    public async Task<(bool Success, string Message)> DeleteBankAsync(int id)
    {
        var item = await _context.Banks.FirstOrDefaultAsync(b => b.Id == id);
        if (item == null) return (false, "Bank not found or already removed.");

        if (item.LedgerId.HasValue)
        {
            var voucherCount = await _context.VoucherItems.CountAsync(v => v.LedgerId == item.LedgerId.Value);
            if (voucherCount > 0)
                return (false, $"Bank '{item.BankName}' cannot be deleted because related accounting transactions exist.");
        }

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Bank '{item.BankName}' deleted successfully.");
    }

    // Tax
    public async Task<List<Tax>> GetTaxesAsync()
    {
        return await _context.Taxes.Where(t => t.IsActive).ToListAsync();
    }

    public async Task<Tax?> GetTaxByIdAsync(int id)
    {
        return await _context.Taxes.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tax> SaveTaxAsync(Tax tax)
    {
        if (string.IsNullOrWhiteSpace(tax.TaxName))
            throw new InvalidOperationException("Tax Name is required.");
        if (tax.TaxPercentage < 0 || tax.TaxPercentage > 100)
            throw new InvalidOperationException("Tax Percentage must be between 0% and 100%.");

        tax.TaxName = tax.TaxName.Trim();

        var duplicate = await _context.Taxes
            .AnyAsync(t => t.TaxName.ToLower() == tax.TaxName.ToLower() && t.Id != tax.Id && t.IsActive);
        if (duplicate)
            throw new InvalidOperationException($"Tax configuration '{tax.TaxName}' already exists.");

        if (tax.Id == 0)
        {
            _context.Taxes.Add(tax);
        }
        else
        {
            await UpdateEntityAsync(tax);
        }
        await _context.SaveChangesAsync();
        return tax;
    }

    public async Task<(bool Success, string Message)> DeleteTaxAsync(int id)
    {
        var item = await _context.Taxes.FirstOrDefaultAsync(t => t.Id == id);
        if (item == null) return (false, "Tax setting not found or already removed.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Tax '{item.TaxName}' deleted successfully.");
    }

    // Payment Mode
    public async Task<List<PaymentMode>> GetPaymentModesAsync()
    {
        return await _context.PaymentModes.Where(pm => pm.IsActive).ToListAsync();
    }

    public async Task<PaymentMode?> GetPaymentModeByIdAsync(int id)
    {
        return await _context.PaymentModes.FirstOrDefaultAsync(pm => pm.Id == id);
    }

    public async Task<PaymentMode> SavePaymentModeAsync(PaymentMode mode)
    {
        if (string.IsNullOrWhiteSpace(mode.ModeName))
            throw new InvalidOperationException("Payment Mode Name is required.");

        mode.ModeName = mode.ModeName.Trim();

        var duplicate = await _context.PaymentModes
            .AnyAsync(m => m.ModeName.ToLower() == mode.ModeName.ToLower() && m.Id != mode.Id && m.IsActive);
        if (duplicate)
            throw new InvalidOperationException($"Payment Mode '{mode.ModeName}' already exists.");

        if (mode.Id == 0)
        {
            _context.PaymentModes.Add(mode);
        }
        else
        {
            await UpdateEntityAsync(mode);
        }
        await _context.SaveChangesAsync();
        return mode;
    }

    public async Task<(bool Success, string Message)> DeletePaymentModeAsync(int id)
    {
        var item = await _context.PaymentModes.FirstOrDefaultAsync(pm => pm.Id == id);
        if (item == null) return (false, "Payment Mode not found or already removed.");

        item.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, $"Payment Mode '{item.ModeName}' deleted successfully.");
    }

    public async Task<(int successCount, List<string> errors)> BulkUploadProductsAsync(System.IO.Stream fileStream)
    {
        var errors = new List<string>();
        var successCount = 0;

        try
        {
            var rows = MiniExcel.Query(fileStream, useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
            
            // Pre-load all lookups for performance
            var categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            var brands = await _context.Brands.Where(b => b.IsActive).ToListAsync();
            var units = await _context.Units.Where(u => u.IsActive).ToListAsync();
            var warehouses = await _context.Warehouses.Where(w => w.IsActive).ToListAsync();
            var existingProducts = await _context.Products.Where(p => p.IsActive).ToListAsync();

            var rowNum = 1;
            foreach (var row in rows)
            {
                rowNum++;
                var dict = row.ToDictionary(k => k.Key.ToLower().Replace(" ", "").Replace("_", ""), v => v.Value);
                
                string getValue(string key) => dict.TryGetValue(key, out var val) ? val?.ToString()?.Trim() ?? "" : "";

                var productName = getValue("productname");
                if (string.IsNullOrEmpty(productName)) productName = getValue("descriptionofgoods");
                if (string.IsNullOrEmpty(productName)) productName = getValue("description");

                var productCode = getValue("productcode");
                if (string.IsNullOrEmpty(productCode))
                {
                    var hsn = getValue("hsnsac");
                    if (string.IsNullOrEmpty(hsn)) hsn = getValue("hsncode");
                    var brand = getValue("brand");
                    if (!string.IsNullOrEmpty(hsn) && !string.IsNullOrEmpty(brand))
                    {
                        productCode = $"{brand}-{hsn}";
                    }
                    else if (!string.IsNullOrEmpty(hsn))
                    {
                        productCode = hsn;
                    }
                    else if (!string.IsNullOrEmpty(productName))
                    {
                        productCode = "PRD-" + Math.Abs(productName.GetHashCode()).ToString();
                    }
                }

                if (string.IsNullOrEmpty(productCode) || string.IsNullOrEmpty(productName))
                {
                    errors.Add($"Row {rowNum}: ProductCode (or HSN) and ProductName (or Description) are required.");
                    continue;
                }

                // Category
                var categoryName = getValue("category");
                int? categoryId = null;
                if (!string.IsNullOrEmpty(categoryName))
                {
                    var cat = categories.FirstOrDefault(c => c.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                    if (cat == null)
                    {
                        cat = new Category { CategoryName = categoryName };
                        _context.Categories.Add(cat);
                        await _context.SaveChangesAsync();
                        categories.Add(cat); // add to local list
                    }
                    categoryId = cat.Id;
                }

                // Brand
                var brandName = getValue("brand");
                int? brandId = null;
                if (!string.IsNullOrEmpty(brandName))
                {
                    var br = brands.FirstOrDefault(b => b.BrandName.Equals(brandName, StringComparison.OrdinalIgnoreCase));
                    if (br == null)
                    {
                        br = new Brand { BrandName = brandName };
                        _context.Brands.Add(br);
                        await _context.SaveChangesAsync();
                        brands.Add(br); // add to local list
                    }
                    brandId = br.Id;
                }

                // Unit
                var unitName = getValue("unit");
                int? unitId = null;
                if (!string.IsNullOrEmpty(unitName))
                {
                    var un = units.FirstOrDefault(u => u.UnitName.Equals(unitName, StringComparison.OrdinalIgnoreCase));
                    if (un == null)
                    {
                        un = new Unit { UnitName = unitName, UnitSymbol = unitName.Length > 3 ? unitName.Substring(0, 3) : unitName };
                        _context.Units.Add(un);
                        await _context.SaveChangesAsync();
                        units.Add(un);
                    }
                    unitId = un.Id;
                }

                // Warehouse
                var warehouseName = getValue("warehouse");
                int? warehouseId = null;
                if (!string.IsNullOrEmpty(warehouseName))
                {
                    var wh = warehouses.FirstOrDefault(w => w.WarehouseName.Equals(warehouseName, StringComparison.OrdinalIgnoreCase));
                    if (wh == null)
                    {
                        wh = new Warehouse { WarehouseName = warehouseName, WarehouseCode = warehouseName.Length > 4 ? warehouseName.Substring(0, 4).ToUpper() : warehouseName.ToUpper() };
                        _context.Warehouses.Add(wh);
                        await _context.SaveChangesAsync();
                        warehouses.Add(wh);
                    }
                    warehouseId = wh.Id;
                }

                // Decimal Parse helper
                decimal parseDecimal(string key) => decimal.TryParse(getValue(key), out var d) ? d : 0;

                var prod = existingProducts.FirstOrDefault(p => p.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase));
                bool isNew = false;
                if (prod == null)
                {
                    prod = new Product { ProductCode = productCode };
                    isNew = true;
                }

                prod.ProductName = productName;
                prod.CategoryId = categoryId;
                prod.BrandId = brandId;
                prod.UnitId = unitId;
                prod.WarehouseId = warehouseId;

                var hsnVal = getValue("hsncode");
                if (string.IsNullOrEmpty(hsnVal)) hsnVal = getValue("hsnsac");
                prod.HSNCode = hsnVal;

                prod.Barcode = getValue("barcode");

                var descriptionVal = getValue("description");
                if (string.IsNullOrEmpty(descriptionVal)) descriptionVal = getValue("descriptionofgoods");
                prod.Description = descriptionVal;

                var purchasePrice = parseDecimal("purchaseprice");
                if (purchasePrice == 0) purchasePrice = parseDecimal("rate(excl.tax)");
                if (purchasePrice == 0) purchasePrice = parseDecimal("rate(incl.tax)") / 1.18m; // fallback 18%
                prod.PurchasePrice = purchasePrice;

                var salesPrice = parseDecimal("salesprice");
                if (salesPrice == 0) salesPrice = Math.Round(purchasePrice * 1.25m, 2);
                prod.SalesPrice = salesPrice;

                var mrp = parseDecimal("mrp");
                if (mrp == 0) mrp = Math.Round(purchasePrice * 1.30m, 2);
                prod.MRP = mrp;

                var discount = parseDecimal("discount");
                if (discount == 0) discount = parseDecimal("disc%");
                prod.Discount = discount;

                var gstPercentage = parseDecimal("gstpercentage");
                if (gstPercentage == 0) gstPercentage = 18; // default
                prod.GSTPercentage = gstPercentage;

                var openingStock = parseDecimal("openingstock");
                if (openingStock == 0) openingStock = parseDecimal("quantity");
                prod.OpeningStock = openingStock;

                prod.MinimumStock = parseDecimal("minimumstock");
                prod.MaximumStock = parseDecimal("maximumstock");
                prod.ReorderLevel = parseDecimal("reorderlevel");
                
                if (isNew)
                {
                    prod.CurrentStock = prod.OpeningStock;
                    _context.Products.Add(prod);
                }
                else
                {
                    _context.Products.Update(prod);
                }

                await _context.SaveChangesAsync();
                successCount++;
            }
        }
        catch (Exception ex)
        {
            errors.Add("Error parsing CSV: " + ex.Message);
        }

        return (successCount, errors);
    }

    public async Task<List<ImportProductPreviewDto>> PreviewImportAsync(System.IO.Stream fileStream, string fileExtension)
    {
        var list = new List<ImportProductPreviewDto>();

        if (fileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            list = _pdfParserService.ParseProductsFromPdf(fileStream);
        }
        else if (fileExtension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) || 
                 fileExtension.Equals(".xls", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var ms = new System.IO.MemoryStream();
                await fileStream.CopyToAsync(ms);

                List<string> sheetNames = new List<string>();
                try
                {
                    ms.Position = 0;
                    sheetNames = MiniExcel.GetSheetNames(ms).ToList();
                }
                catch
                {
                    sheetNames.Add("Sheet1");
                }

                foreach (var sheetName in sheetNames)
                {
                    if (sheetName.Equals("Summary", StringComparison.OrdinalIgnoreCase))
                        continue;

                    ms.Position = 0;
                    var rows = MiniExcel.Query(ms, sheetName: sheetName, useHeaderRow: false)
                                        .Cast<IDictionary<string, object>>()
                                        .ToList();

                    Dictionary<string, string>? mappings = null;
                    int headerRowIndex = -1;

                    // Scan first 15 rows for the header
                    for (int i = 0; i < Math.Min(rows.Count, 15); i++)
                    {
                        if (IsHeaderRow(rows[i], out var tempMappings))
                        {
                            mappings = tempMappings;
                            headerRowIndex = i;
                            break;
                        }
                    }

                    if (mappings == null) continue;

                    for (int i = headerRowIndex + 1; i < rows.Count; i++)
                    {
                        var row = rows[i];
                        string getValue(string logicKey)
                        {
                            if (mappings.TryGetValue(logicKey, out var colKey) && row.TryGetValue(colKey, out var val))
                            {
                                return val?.ToString()?.Trim() ?? "";
                            }
                            return "";
                        }

                        decimal parseDecimal(string logicKey)
                        {
                            var valStr = getValue(logicKey);
                            return decimal.TryParse(valStr, out var d) ? d : 0;
                        }

                        var productName = getValue("productname");
                        if (string.IsNullOrEmpty(productName)) continue;

                        var brandName = getValue("brand");
                        var categoryName = getValue("category");
                        if (string.IsNullOrEmpty(categoryName))
                        {
                            categoryName = (productName.Contains("chainsaw", StringComparison.OrdinalIgnoreCase) || 
                                            productName.Contains("chain saw", StringComparison.OrdinalIgnoreCase)) 
                                            ? "Chain saw" 
                                            : sheetName;
                        }

                        var hsnCode = getValue("hsn");
                        var purchasePrice = parseDecimal("purchaseprice");
                        var salesPrice = parseDecimal("salesprice");
                        var mrp = parseDecimal("mrp");
                        
                        if (purchasePrice == 0) purchasePrice = salesPrice;
                        if (salesPrice == 0) salesPrice = purchasePrice;
                        if (mrp == 0) mrp = salesPrice;

                        var productCode = getValue("productcode");
                        if (string.IsNullOrEmpty(productCode))
                        {
                            if (!string.IsNullOrEmpty(hsnCode) && !string.IsNullOrEmpty(brandName))
                                productCode = $"{brandName}-{hsnCode}";
                            else if (!string.IsNullOrEmpty(hsnCode))
                                productCode = hsnCode;
                            else
                                productCode = "PRD-" + Math.Abs(productName.GetHashCode()).ToString();
                        }

                        var gstPercent = parseDecimal("gst");
                        if (gstPercent == 0) gstPercent = 18;

                        list.Add(new ImportProductPreviewDto
                        {
                            ProductCode = productCode,
                            ProductName = productName,
                            CategoryName = categoryName,
                            BrandName = brandName,
                            UnitName = getValue("unit"),
                            WarehouseName = getValue("warehouse"),
                            HSNCode = hsnCode,
                            PurchasePrice = purchasePrice,
                            SalesPrice = salesPrice,
                            MRP = mrp,
                            Discount = parseDecimal("discount"),
                            GSTPercentage = gstPercent,
                            OpeningStock = parseDecimal("quantity"),
                            Description = getValue("description")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Excel preview error: " + ex.Message);
            }
        }

        // Compare with database products to detect matches
        var dbProducts = await _context.Products.Where(p => p.IsActive).ToListAsync();

        foreach (var item in list)
        {
            // Try to find exact match by name
            var exactMatch = dbProducts.FirstOrDefault(p =>
                p.ProductName.Equals(item.ProductName, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                item.ExistsInDb = true;
                item.ExistingProductId = exactMatch.Id;
                item.MatchStatus = "ExactMatch";
                item.ProductCode = exactMatch.ProductCode; // Keep database product code
                continue;
            }

            // Try to find similar match
            Product? bestSimilarMatch = null;
            double maxSimilarity = 0.0;

            foreach (var p in dbProducts)
            {
                double sim = GetLevenshteinSimilarity(item.ProductName, p.ProductName);
                if (sim > 0.75 && sim > maxSimilarity)
                {
                    maxSimilarity = sim;
                    bestSimilarMatch = p;
                }
            }

            if (bestSimilarMatch != null)
            {
                item.ExistsInDb = true;
                item.ExistingProductId = bestSimilarMatch.Id;
                item.MatchStatus = "SimilarMatch";
                item.SimilarMatchedName = bestSimilarMatch.ProductName;
            }
            else
            {
                item.ExistsInDb = false;
                item.MatchStatus = "New";
            }
        }

        return list;
    }

    private List<IDictionary<string, object>> MiniExcelQuery(System.IO.Stream stream)
    {
        return MiniExcel.Query(stream, useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
    }

    private bool IsHeaderRow(IDictionary<string, object> dict, out Dictionary<string, string> mappings)
    {
        mappings = new Dictionary<string, string>();
        bool foundName = false;

        foreach (var kvp in dict)
        {
            var valStr = kvp.Value?.ToString()?.Trim()?.ToLower()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("/", "")
                .Replace("\\", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(".", "")
                .Replace("%", "");

            if (string.IsNullOrEmpty(valStr)) continue;

            if (valStr == "descriptionofgoods" || valStr == "productname" || valStr == "name" || valStr == "itemname" || valStr == "description")
            {
                mappings["productname"] = kvp.Key;
                foundName = true;
            }
            else if (valStr == "brand" || valStr == "brandname" || valStr == "make")
            {
                mappings["brand"] = kvp.Key;
            }
            else if (valStr == "category" || valStr == "categoryname" || valStr == "group")
            {
                mappings["category"] = kvp.Key;
            }
            else if (valStr == "hsnsac" || valStr == "hsncode" || valStr == "hsn")
            {
                mappings["hsn"] = kvp.Key;
            }
            else if (valStr == "quantity" || valStr == "qty" || valStr == "openingstock" || valStr == "stock")
            {
                mappings["quantity"] = kvp.Key;
            }
            else if (valStr == "unit" || valStr == "unitname" || valStr == "uom")
            {
                mappings["unit"] = kvp.Key;
            }
            else if (valStr == "rateexcltax" || valStr == "rateexcltax" || valStr == "rateexcl.tax" || valStr == "rateexcl" || valStr == "purchaseprice" || valStr == "purchase" || valStr == "rate")
            {
                if (!mappings.ContainsKey("purchaseprice") || valStr.Contains("excl"))
                {
                    mappings["purchaseprice"] = kvp.Key;
                }
            }
            else if (valStr == "rateincltax" || valStr == "rateincltax" || valStr == "rateincl.tax" || valStr == "rateincl" || valStr == "salesprice" || valStr == "sales" || valStr == "mrp")
            {
                mappings["salesprice"] = kvp.Key;
            }
            else if (valStr == "discount" || valStr == "disc" || valStr == "disc%")
            {
                mappings["discount"] = kvp.Key;
            }
            else if (valStr == "gstpercentage" || valStr == "gst" || valStr == "gst%")
            {
                mappings["gst"] = kvp.Key;
            }
            else if (valStr == "warehouse" || valStr == "warehousename" || valStr == "location")
            {
                mappings["warehouse"] = kvp.Key;
            }
            else if (valStr == "productcode" || valStr == "code" || valStr == "itemcode")
            {
                mappings["productcode"] = kvp.Key;
            }
            else if (valStr == "description" || valStr == "remarks")
            {
                mappings["description"] = kvp.Key;
            }
        }

        return foundName;
    }

    public async Task<List<ProductImportResultDto>> CommitImportAsync(List<ImportProductCommitDto> items)
    {
        var results = new List<ProductImportResultDto>();
        if (items == null || !items.Any()) return results;

        // Load all active lookups to check duplicates
        var categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
        var brands = await _context.Brands.Where(b => b.IsActive).ToListAsync();
        var units = await _context.Units.Where(u => u.IsActive).ToListAsync();
        var warehouses = await _context.Warehouses.Where(w => w.IsActive).ToListAsync();
        var products = await _context.Products.Where(p => p.IsActive).ToListAsync();

        int importedCount = 0;

        foreach (var item in items)
        {
            if (item.ActionType == "Ignore") continue;

            // 1. Get or Create Category
            int? categoryId = null;
            if (!string.IsNullOrEmpty(item.CategoryName))
            {
                var cat = categories.FirstOrDefault(c => c.CategoryName.Equals(item.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (cat == null)
                {
                    cat = new Category { CategoryName = item.CategoryName.Trim(), CreatedAt = DateTime.Now, IsActive = true };
                    _context.Categories.Add(cat);
                    await _context.SaveChangesAsync();
                    categories.Add(cat);
                }
                categoryId = cat.Id;
            }

            // 2. Get or Create Brand
            int? brandId = null;
            if (!string.IsNullOrEmpty(item.BrandName))
            {
                var br = brands.FirstOrDefault(b => b.BrandName.Equals(item.BrandName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (br == null)
                {
                    br = new Brand { BrandName = item.BrandName.Trim(), CreatedAt = DateTime.Now, IsActive = true };
                    _context.Brands.Add(br);
                    await _context.SaveChangesAsync();
                    brands.Add(br);
                }
                brandId = br.Id;
            }

            // 3. Get or Create Unit
            int? unitId = null;
            if (!string.IsNullOrEmpty(item.UnitName))
            {
                var un = units.FirstOrDefault(u => 
                    u.UnitName.Equals(item.UnitName.Trim(), StringComparison.OrdinalIgnoreCase) || 
                    u.UnitSymbol.Equals(item.UnitName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (un == null)
                {
                    var cleanName = item.UnitName.Trim();
                    un = new Unit 
                    { 
                        UnitName = cleanName, 
                        UnitSymbol = cleanName.Length > 3 ? cleanName.Substring(0, 3).ToUpper() : cleanName.ToUpper(),
                        CreatedAt = DateTime.Now, 
                        IsActive = true 
                    };
                    _context.Units.Add(un);
                    await _context.SaveChangesAsync();
                    units.Add(un);
                }
                unitId = un.Id;
            }

            // 4. Get or Create Warehouse
            int? warehouseId = null;
            if (!string.IsNullOrEmpty(item.WarehouseName))
            {
                var wh = warehouses.FirstOrDefault(w => w.WarehouseName.Equals(item.WarehouseName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (wh == null)
                {
                    var cleanName = item.WarehouseName.Trim();
                    wh = new Warehouse 
                    { 
                        WarehouseName = cleanName, 
                        WarehouseCode = cleanName.Length > 4 ? cleanName.Substring(0, 4).ToUpper() : cleanName.ToUpper(),
                        CreatedAt = DateTime.Now, 
                        IsActive = true 
                    };
                    _context.Warehouses.Add(wh);
                    await _context.SaveChangesAsync();
                    warehouses.Add(wh);
                }
                warehouseId = wh.Id;
            }

            // Set defaults if still null
            if (categoryId == null && categories.Any()) categoryId = categories.First().Id;
            if (brandId == null && brands.Any()) brandId = brands.First().Id;
            if (unitId == null && units.Any()) unitId = units.First().Id;
            if (warehouseId == null && warehouses.Any()) warehouseId = warehouses.First().Id;

            Product? prod = null;
            bool isNew = false;

            if (item.ActionType == "UpdateExisting" && item.ExistingProductId.HasValue)
            {
                prod = await _context.Products.FindAsync(item.ExistingProductId.Value);
            }

            // Deduplication Fallback: Search by exact product name ONLY
            if (prod == null)
            {
                var trimmedName = item.ProductName.Trim();
                prod = await _context.Products.FirstOrDefaultAsync(p => p.IsActive &&
                    p.ProductName.ToLower() == trimmedName.ToLower());
            }

            if (prod == null)
            {
                prod = new Product { CreatedAt = DateTime.Now, IsActive = true };
                isNew = true;
            }

            prod.ProductName = item.ProductName.Trim();
            prod.CategoryId = categoryId;
            prod.BrandId = brandId;
            prod.UnitId = unitId;
            prod.WarehouseId = warehouseId;
            prod.HSNCode = item.HSNCode;
            prod.PurchasePrice = item.PurchasePrice;
            prod.SalesPrice = item.SalesPrice == 0 ? Math.Round(item.PurchasePrice * 1.25m, 2) : item.SalesPrice;
            prod.MRP = item.MRP == 0 ? Math.Round(item.PurchasePrice * 1.30m, 2) : item.MRP;
            prod.GSTPercentage = item.GSTPercentage == 0 ? 18 : item.GSTPercentage;
            prod.Discount = item.Discount;
            prod.Description = item.Description;

            if (isNew)
            {
                prod.ProductCode = await GenerateUniqueProductCodeAsync(item.BrandName, item.HSNCode, item.ProductName);
                prod.OpeningStock = item.OpeningStock;
                prod.CurrentStock = item.OpeningStock;
                _context.Products.Add(prod);
            }
            else
            {
                if (!string.IsNullOrEmpty(item.ProductCode) && !prod.ProductCode.Equals(item.ProductCode.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    var codeExists = await _context.Products.AnyAsync(p => p.Id != prod.Id && p.ProductCode == item.ProductCode.Trim());
                    if (!codeExists)
                    {
                        prod.ProductCode = item.ProductCode.Trim();
                    }
                }
                prod.UpdatedAt = DateTime.Now;
                _context.Products.Update(prod);
            }

            await _context.SaveChangesAsync();
            importedCount++;

            results.Add(new ProductImportResultDto
            {
                ProductId = prod.Id,
                ProductName = prod.ProductName,
                ProductCode = prod.ProductCode,
                Quantity = item.OpeningStock,
                Rate = prod.PurchasePrice,
                CategoryId = prod.CategoryId
            });
        }

        return results;
    }

    private async Task<string> GenerateUniqueProductCodeAsync(string? brandName, string? hsnCode, string productName)
    {
        string baseCode;
        if (!string.IsNullOrEmpty(brandName) && !string.IsNullOrEmpty(hsnCode))
        {
            baseCode = $"{brandName.Trim()}-{hsnCode.Trim()}";
        }
        else if (!string.IsNullOrEmpty(hsnCode))
        {
            baseCode = hsnCode.Trim();
        }
        else if (productName.StartsWith("P-", StringComparison.OrdinalIgnoreCase))
        {
            var parts = productName.Split(' ');
            baseCode = parts[0];
        }
        else
        {
            baseCode = "PRD-" + Math.Abs(productName.GetHashCode()).ToString();
        }

        // Clean the baseCode to be alphanumeric/dashes
        baseCode = new string(baseCode.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        // Check if exists in DB, append suffix if it does
        string uniqueCode = baseCode;
        int suffix = 1;
        while (await _context.Products.AnyAsync(p => p.ProductCode == uniqueCode))
        {
            uniqueCode = $"{baseCode}-{suffix}";
            suffix++;
        }
        return uniqueCode;
    }

    private static double GetLevenshteinSimilarity(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 1.0 : 0.0;
        if (string.IsNullOrEmpty(t)) return 0.0;

        s = s.ToLowerInvariant();
        t = t.ToLowerInvariant();

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        int maxLength = Math.Max(s.Length, t.Length);
        return 1.0 - ((double)d[n, m] / maxLength);
    }
}
