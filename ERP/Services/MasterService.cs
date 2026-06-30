using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;
using System.Globalization;
using MiniExcelLibs;

namespace ERP.Services;

public class MasterService : IMasterService
{
    private readonly AppDbContext _context;

    public MasterService(AppDbContext context)
    {
        _context = context;
    }

    private async Task UpdateEntityAsync<T>(T entity) where T : BaseEntity
    {
        var existing = await _context.Set<T>().FindAsync(entity.Id);
        if (existing != null)
        {
            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.IsActive = existing.IsActive;

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
    }

    // Company
    public async Task<Company?> GetCompanyAsync()
    {
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

    public async Task<string> GetNextBillNumberPreviewAsync(string billType)
    {
        var company = await GetCompanyAsync() ?? new Company();
        return FormatBillNumber(company, billType, billType.Equals("purchase", StringComparison.OrdinalIgnoreCase)
            ? company.PurchaseBillNextNumber
            : company.SalesBillNextNumber);
    }

    public async Task<string> ReserveNextBillNumberAsync(string billType)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.IsActive)
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
        return await _context.Customers.FindAsync(id);
    }

    public async Task<Customer> SaveCustomerAsync(Customer customer)
    {
        if (customer.Id == 0)
        {
            _context.Customers.Add(customer);
        }
        else
        {
            await UpdateEntityAsync(customer);
        }
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task DeleteCustomerAsync(int id)
    {
        var item = await _context.Customers.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    // Supplier
    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        return await _context.Suppliers.Where(s => s.IsActive).ToListAsync();
    }

    public async Task<Supplier?> GetSupplierByIdAsync(int id)
    {
        return await _context.Suppliers.FindAsync(id);
    }

    public async Task<Supplier> SaveSupplierAsync(Supplier supplier)
    {
        if (supplier.Id == 0)
        {
            _context.Suppliers.Add(supplier);
        }
        else
        {
            await UpdateEntityAsync(supplier);
        }
        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task DeleteSupplierAsync(int id)
    {
        var item = await _context.Suppliers.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
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
        if (product.Id == 0)
        {
            _context.Products.Add(product);
        }
        else
        {
            await UpdateEntityAsync(product);
        }
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task DeleteProductAsync(int id)
    {
        var item = await _context.Products.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    // Category
    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await _context.Categories.Where(c => c.IsActive).ToListAsync();
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories.FindAsync(id);
    }

    public async Task<Category> SaveCategoryAsync(Category category)
    {
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

    public async Task DeleteCategoryAsync(int id)
    {
        var item = await _context.Categories.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    // Brand
    public async Task<List<Brand>> GetBrandsAsync()
    {
        return await _context.Brands.Where(b => b.IsActive).ToListAsync();
    }

    public async Task<Brand?> GetBrandByIdAsync(int id)
    {
        return await _context.Brands.FindAsync(id);
    }

    public async Task<Brand> SaveBrandAsync(Brand brand)
    {
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

    public async Task DeleteBrandAsync(int id)
    {
        var item = await _context.Brands.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    // Unit
    public async Task<List<Unit>> GetUnitsAsync()
    {
        return await _context.Units.Where(u => u.IsActive).ToListAsync();
    }

    public async Task<Unit?> GetUnitByIdAsync(int id)
    {
        return await _context.Units.FindAsync(id);
    }

    public async Task<Unit> SaveUnitAsync(Unit unit)
    {
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

    public async Task DeleteUnitAsync(int id)
    {
        var item = await _context.Units.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    // Warehouse
    public async Task<List<Warehouse>> GetWarehousesAsync()
    {
        return await _context.Warehouses.Where(w => w.IsActive).ToListAsync();
    }

    public async Task<Warehouse?> GetWarehouseByIdAsync(int id)
    {
        return await _context.Warehouses.FindAsync(id);
    }

    public async Task<Warehouse> SaveWarehouseAsync(Warehouse warehouse)
    {
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

    public async Task DeleteWarehouseAsync(int id)
    {
        var item = await _context.Warehouses.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    // Employee
    public async Task<List<Employee>> GetEmployeesAsync()
    {
        return await _context.Employees.Where(e => e.IsActive).ToListAsync();
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        return await _context.Employees.FindAsync(id);
    }

    public async Task<Employee> SaveEmployeeAsync(Employee employee)
    {
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

    public async Task DeleteEmployeeAsync(int id)
    {
        var item = await _context.Employees.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    // Account Group
    public async Task<List<AccountGroup>> GetAccountGroupsAsync()
    {
        return await _context.AccountGroups.Where(ag => ag.IsActive).ToListAsync();
    }

    public async Task<AccountGroup?> GetAccountGroupByIdAsync(int id)
    {
        return await _context.AccountGroups.FindAsync(id);
    }

    public async Task<AccountGroup> SaveAccountGroupAsync(AccountGroup group)
    {
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

    public async Task DeleteAccountGroupAsync(int id)
    {
        var item = await _context.AccountGroups.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
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

    public async Task DeleteLedgerAsync(int id)
    {
        var item = await _context.Ledgers.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    // Bank
    public async Task<List<Bank>> GetBanksAsync()
    {
        return await _context.Banks.Include(b => b.Ledger).Where(b => b.IsActive).ToListAsync();
    }

    public async Task<Bank?> GetBankByIdAsync(int id)
    {
        return await _context.Banks.FindAsync(id);
    }

    public async Task<Bank> SaveBankAsync(Bank bank)
    {
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

    public async Task DeleteBankAsync(int id)
    {
        var item = await _context.Banks.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    // Tax
    public async Task<List<Tax>> GetTaxesAsync()
    {
        return await _context.Taxes.Where(t => t.IsActive).ToListAsync();
    }

    public async Task<Tax?> GetTaxByIdAsync(int id)
    {
        return await _context.Taxes.FindAsync(id);
    }

    public async Task<Tax> SaveTaxAsync(Tax tax)
    {
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

    public async Task DeleteTaxAsync(int id)
    {
        var item = await _context.Taxes.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    // Payment Mode
    public async Task<List<PaymentMode>> GetPaymentModesAsync()
    {
        return await _context.PaymentModes.Where(pm => pm.IsActive).ToListAsync();
    }

    public async Task<PaymentMode?> GetPaymentModeByIdAsync(int id)
    {
        return await _context.PaymentModes.FindAsync(id);
    }

    public async Task<PaymentMode> SavePaymentModeAsync(PaymentMode mode)
    {
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

    public async Task DeletePaymentModeAsync(int id)
    {
        var item = await _context.PaymentModes.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
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

                var productCode = getValue("productcode");
                var productName = getValue("productname");

                if (string.IsNullOrEmpty(productCode) || string.IsNullOrEmpty(productName))
                {
                    errors.Add($"Row {rowNum}: ProductCode and ProductName are required.");
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
                prod.HSNCode = getValue("hsncode");
                prod.Barcode = getValue("barcode");
                prod.Description = getValue("description");
                
                prod.PurchasePrice = parseDecimal("purchaseprice");
                prod.SalesPrice = parseDecimal("salesprice");
                prod.MRP = parseDecimal("mrp");
                prod.Discount = parseDecimal("discount");
                prod.GSTPercentage = parseDecimal("gstpercentage");
                prod.OpeningStock = parseDecimal("openingstock");
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
}
