using Microsoft.AspNetCore.Identity;
using ERP.Models;

namespace ERP.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // 1. Roles
        string[] roles = { "Super Admin", "Admin", "Sales User", "Purchase User", "Accountant", "Manager" };
        foreach (var r in roles)
        {
            if (!await roleManager.RoleExistsAsync(r))
            {
                await roleManager.CreateAsync(new IdentityRole(r));
            }
        }

        // 2. Default Admin User
        var adminEmail = "admin@smarterp.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrator",
                Mobile = "9876543210",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Super Admin");
            }
        }

        // 3. Company
        if (!context.Companies.Any())
        {
            context.Companies.Add(new Company
            {
                CompanyName = "SmartERP Enterprises",
                CompanyCode = "SEE01",
                Address = "123 Business Boulevard",
                City = "Chennai",
                State = "Tamil Nadu",
                Country = "India",
                Phone = "044-1234567",
                Email = "info@smarterp.com",
                GSTNumber = "33AAAAA0000A1Z1",
                PANNumber = "AAAAA0000A"
            });
            await context.SaveChangesAsync();
        }

        // 4. Account Groups
        if (!context.AccountGroups.Any())
        {
            var groups = new List<AccountGroup>
            {
                new() { GroupName = "Capital Account", GroupType = "Equity", Description = "Owner's capital" },
                new() { GroupName = "Sundry Debtors", GroupType = "Asset", Description = "Customers ledger group" },
                new() { GroupName = "Sundry Creditors", GroupType = "Liability", Description = "Suppliers ledger group" },
                new() { GroupName = "Sales Accounts", GroupType = "Income", Description = "Sales revenue accounts" },
                new() { GroupName = "Purchase Accounts", GroupType = "Expense", Description = "Purchase accounts" },
                new() { GroupName = "Direct Expenses", GroupType = "Expense", Description = "Manufacturing & direct costs" },
                new() { GroupName = "Indirect Expenses", GroupType = "Expense", Description = "Overheads & administrative expenses" },
                new() { GroupName = "Bank Accounts", GroupType = "Asset", Description = "Bank ledgers" },
                new() { GroupName = "Cash-in-hand", GroupType = "Asset", Description = "Physical cash accounts" },
                new() { GroupName = "Duties & Taxes", GroupType = "Liability", Description = "Tax accounts (GST, VAT, etc.)" }
            };
            context.AccountGroups.AddRange(groups);
            await context.SaveChangesAsync();
        }

        // 5. Default Ledgers
        if (!context.Ledgers.Any())
        {
            var cashGroup = context.AccountGroups.First(g => g.GroupName == "Cash-in-hand");
            var salesGroup = context.AccountGroups.First(g => g.GroupName == "Sales Accounts");
            var purchaseGroup = context.AccountGroups.First(g => g.GroupName == "Purchase Accounts");
            var taxGroup = context.AccountGroups.First(g => g.GroupName == "Duties & Taxes");

            context.Ledgers.AddRange(
                new Ledger { LedgerCode = "CASH", LedgerName = "Cash Account", AccountGroupId = cashGroup.Id, IsSystemLedger = true, BalanceType = "Dr" },
                new Ledger { LedgerCode = "SALES", LedgerName = "Sales Account", AccountGroupId = salesGroup.Id, IsSystemLedger = true, BalanceType = "Cr" },
                new Ledger { LedgerCode = "PURCHASE", LedgerName = "Purchase Account", AccountGroupId = purchaseGroup.Id, IsSystemLedger = true, BalanceType = "Dr" },
                new Ledger { LedgerCode = "CGST9", LedgerName = "CGST @ 9%", AccountGroupId = taxGroup.Id, IsSystemLedger = true, BalanceType = "Cr" },
                new Ledger { LedgerCode = "SGST9", LedgerName = "SGST @ 9%", AccountGroupId = taxGroup.Id, IsSystemLedger = true, BalanceType = "Cr" },
                new Ledger { LedgerCode = "IGST18", LedgerName = "IGST @ 18%", AccountGroupId = taxGroup.Id, IsSystemLedger = true, BalanceType = "Cr" }
            );
            await context.SaveChangesAsync();
        }

        // 6. Tax Masters
        if (!context.Taxes.Any())
        {
            context.Taxes.AddRange(
                new Tax { TaxName = "GST 18%", TaxPercentage = 18, CGSTPercentage = 9, SGSTPercentage = 9, IGSTPercentage = 18 },
                new Tax { TaxName = "GST 12%", TaxPercentage = 12, CGSTPercentage = 6, SGSTPercentage = 6, IGSTPercentage = 12 },
                new Tax { TaxName = "GST 5%", TaxPercentage = 5, CGSTPercentage = 2.5m, SGSTPercentage = 2.5m, IGSTPercentage = 5 },
                new Tax { TaxName = "GST Exempted", TaxPercentage = 0, CGSTPercentage = 0, SGSTPercentage = 0, IGSTPercentage = 0 }
            );
        }

        // 7. Payment Modes
        if (!context.PaymentModes.Any())
        {
            context.PaymentModes.AddRange(
                new PaymentMode { ModeName = "Cash", ModeType = "Cash" },
                new PaymentMode { ModeName = "Bank Transfer", ModeType = "Bank" },
                new PaymentMode { ModeName = "Credit Card", ModeType = "Card" },
                new PaymentMode { ModeName = "UPI / QR Code", ModeType = "UPI" }
            );
        }

        // 8. Categories, Brands, Units, Warehouses
        if (!context.Categories.Any())
        {
            context.Categories.AddRange(
                new Category { CategoryName = "Electronics" },
                new Category { CategoryName = "Office Supplies" },
                new Category { CategoryName = "Services" }
            );
        }

        if (!context.Brands.Any())
        {
            context.Brands.AddRange(
                new Brand { BrandName = "Generic" },
                new Brand { BrandName = "HP" },
                new Brand { BrandName = "Dell" }
            );
        }

        if (!context.Units.Any())
        {
            context.Units.AddRange(
                new Unit { UnitName = "Pieces", UnitSymbol = "PCS" },
                new Unit { UnitName = "Box", UnitSymbol = "BOX" },
                new Unit { UnitName = "Hours", UnitSymbol = "HRS" }
            );
        }

        if (!context.Warehouses.Any())
        {
            context.Warehouses.Add(new Warehouse
            {
                WarehouseCode = "MAIN-WH",
                WarehouseName = "Main Central Warehouse",
                City = "Chennai",
                State = "Tamil Nadu"
            });
        }

        await context.SaveChangesAsync();

        // 9. Products
        if (!context.Products.Any())
        {
            var category = context.Categories.First();
            var brand = context.Brands.First();
            var unit = context.Units.First();
            var warehouse = context.Warehouses.First();

            context.Products.AddRange(
                new Product
                {
                    ProductCode = "PRD001",
                    ProductName = "Lenovo ThinkPad Laptop",
                    CategoryId = category.Id,
                    BrandId = brand.Id,
                    UnitId = unit.Id,
                    WarehouseId = warehouse.Id,
                    PurchasePrice = 45000,
                    SalesPrice = 55000,
                    MRP = 60000,
                    GSTPercentage = 18,
                    OpeningStock = 10,
                    CurrentStock = 10,
                    MinimumStock = 2,
                    ReorderLevel = 3
                },
                new Product
                {
                    ProductCode = "PRD002",
                    ProductName = "Wireless Mouse",
                    CategoryId = category.Id,
                    BrandId = brand.Id,
                    UnitId = unit.Id,
                    WarehouseId = warehouse.Id,
                    PurchasePrice = 800,
                    SalesPrice = 1200,
                    MRP = 1500,
                    GSTPercentage = 18,
                    OpeningStock = 50,
                    CurrentStock = 50,
                    MinimumStock = 5,
                    ReorderLevel = 10
                }
            );
            await context.SaveChangesAsync();
        }

        // 10. Customers & Suppliers
        if (!context.Customers.Any())
        {
            context.Customers.Add(new Customer
            {
                CustomerCode = "CUST001",
                CustomerName = "Acme Corp",
                MobileNumber = "9988776655",
                Email = "billing@acme.com",
                GSTNumber = "33ACME0000A1Z1",
                Address = "Tech Park Road",
                City = "Chennai",
                State = "Tamil Nadu",
                CreditLimit = 100000,
                OpeningBalance = 0
            });
        }

        if (!context.Suppliers.Any())
        {
            context.Suppliers.Add(new Supplier
            {
                SupplierCode = "SUPP001",
                SupplierName = "Global Distributors",
                ContactPerson = "John Doe",
                Mobile = "8877665544",
                Email = "sales@globaldist.com",
                Address = "Industrial Zone",
                City = "Mumbai",
                State = "Maharashtra"
            });
        }

        await context.SaveChangesAsync();
    }
}
