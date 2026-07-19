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
                EmailConfirmed = true,
                ClearTextPassword = "Admin@123"
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Super Admin");
            }
        }
        else if (string.IsNullOrEmpty(adminUser.ClearTextPassword))
        {
            adminUser.ClearTextPassword = "Admin@123";
            await userManager.UpdateAsync(adminUser);
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
                new Category { CategoryName = "Power Drills", Description = "Heavy-duty electric and cordless impact drills" },
                new Category { CategoryName = "Angle Grinders", Description = "High-performance handheld metal grinders and cutters" },
                new Category { CategoryName = "Rotary Hammers", Description = "Demolition and rotary hammer concrete drilling machines" },
                new Category { CategoryName = "Cut-off Machines", Description = "Heavy-duty metal cut-off chop saws" },
                new Category { CategoryName = "Circular Saws", Description = "Precision woodworking and panel circular saws" },
                new Category { CategoryName = "Welding Machines", Description = "Portable inverter arc and TIG welding equipment" },
                new Category { CategoryName = "Air Compressors", Description = "High-output industrial air compressor tanks" },
                new Category { CategoryName = "Pressure Washers", Description = "Professional high-pressure cleaning pumps" },
                new Category { CategoryName = "Tool Accessories", Description = "Genuine drill bits, grinding discs, and spares" },
                new Category { CategoryName = "Safety Equipment", Description = "Industrial helmets, welding masks, gloves, and glasses" }
            );
            await context.SaveChangesAsync();
        }

        if (!context.Brands.Any())
        {
            context.Brands.AddRange(
                new Brand { BrandName = "DeWalt" },
                new Brand { BrandName = "Bosch" },
                new Brand { BrandName = "Makita" },
                new Brand { BrandName = "Hitachi" },
                new Brand { BrandName = "Metabo" },
                new Brand { BrandName = "VMR" }
            );
            await context.SaveChangesAsync();
        }

        if (!context.Units.Any())
        {
            context.Units.AddRange(
                new Unit { UnitName = "Pieces", UnitSymbol = "PCS" },
                new Unit { UnitName = "Box", UnitSymbol = "BOX" },
                new Unit { UnitName = "Set", UnitSymbol = "SET" }
            );
            await context.SaveChangesAsync();
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
            await context.SaveChangesAsync();
        }

        // 9. Products
        if (false && !context.Products.Any())
        {
            var categories = context.Categories.ToList();
            var brands = context.Brands.ToList();
            var unitPcs = context.Units.First(u => u.UnitSymbol == "PCS").Id;
            var unitBox = context.Units.First(u => u.UnitSymbol == "BOX").Id;
            var unitSet = context.Units.First(u => u.UnitSymbol == "SET").Id;
            var warehouseId = context.Warehouses.First().Id;

            var drillsCat = categories.First(c => c.CategoryName == "Power Drills").Id;
            var grindersCat = categories.First(c => c.CategoryName == "Angle Grinders").Id;
            var hammersCat = categories.First(c => c.CategoryName == "Rotary Hammers").Id;
            var cutoffCat = categories.First(c => c.CategoryName == "Cut-off Machines").Id;
            var sawsCat = categories.First(c => c.CategoryName == "Circular Saws").Id;
            var weldingCat = categories.First(c => c.CategoryName == "Welding Machines").Id;
            var compressorsCat = categories.First(c => c.CategoryName == "Air Compressors").Id;
            var washersCat = categories.First(c => c.CategoryName == "Pressure Washers").Id;
            var accessoriesCat = categories.First(c => c.CategoryName == "Tool Accessories").Id;
            var safetyCat = categories.First(c => c.CategoryName == "Safety Equipment").Id;

            var dewalt = brands.First(b => b.BrandName == "DeWalt").Id;
            var bosch = brands.First(b => b.BrandName == "Bosch").Id;
            var makita = brands.First(b => b.BrandName == "Makita").Id;
            var hitachi = brands.First(b => b.BrandName == "Hitachi").Id;
            var metabo = brands.First(b => b.BrandName == "Metabo").Id;
            var vmr = brands.First(b => b.BrandName == "VMR").Id;

            context.Products.AddRange(
                // Power Drills
                new Product
                {
                    ProductCode = "VMR-DR001",
                    ProductName = "DeWalt DCD771C2 20V Max Cordless Drill",
                    CategoryId = drillsCat,
                    BrandId = dewalt,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 6200,
                    SalesPrice = 7490,
                    MRP = 8500,
                    GSTPercentage = 18,
                    OpeningStock = 15,
                    CurrentStock = 15,
                    MinimumStock = 3,
                    ReorderLevel = 5,
                    Description = "High performance motor delivers 300 unit watts out (UWO) of power ability completing a wide range of applications.",
                    ImagePath = "https://images.unsplash.com/photo-1504148455328-c376907d081c?auto=format&fit=crop&w=800&q=80"
                },
                new Product
                {
                    ProductCode = "VMR-DR002",
                    ProductName = "Bosch GSB 501 500W Impact Drill",
                    CategoryId = drillsCat,
                    BrandId = bosch,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 2900,
                    SalesPrice = 3890,
                    MRP = 4500,
                    GSTPercentage = 18,
                    OpeningStock = 25,
                    CurrentStock = 25,
                    MinimumStock = 5,
                    ReorderLevel = 10,
                    Description = "Powerful and reliable tool with a compact design. Ergonomic handle design makes it comfortable for overhead work.",
                    ImagePath = "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=800&q=80"
                },
                new Product
                {
                    ProductCode = "VMR-DR003",
                    ProductName = "Makita HP1630 16mm Hammer Drill",
                    CategoryId = drillsCat,
                    BrandId = makita,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 4600,
                    SalesPrice = 5650,
                    MRP = 6200,
                    GSTPercentage = 18,
                    OpeningStock = 12,
                    CurrentStock = 12,
                    MinimumStock = 2,
                    ReorderLevel = 4,
                    Description = "Cylinder-like motor housing and aluminum gear housing cover provide high durability and extended tool lifespan.",
                    ImagePath = "https://images.unsplash.com/photo-1530124566582-ab0510492f27?auto=format&fit=crop&w=800&q=80"
                },
                // Angle Grinders
                new Product
                {
                    ProductCode = "VMR-GR001",
                    ProductName = "Bosch GWS 600 Professional Angle Grinder",
                    CategoryId = grindersCat,
                    BrandId = bosch,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 2500,
                    SalesPrice = 3250,
                    MRP = 3900,
                    GSTPercentage = 18,
                    OpeningStock = 30,
                    CurrentStock = 30,
                    MinimumStock = 6,
                    ReorderLevel = 10,
                    Description = "670W maximum input power with bullet-proof guard for high-level user protection during metal cutting and grinding.",
                    ImagePath = "https://images.unsplash.com/photo-1572981779307-38b8cabb2407?auto=format&fit=crop&w=800&q=80"
                },
                new Product
                {
                    ProductCode = "VMR-GR002",
                    ProductName = "DeWalt DWE4010 4-Inch Angle Grinder",
                    CategoryId = grindersCat,
                    BrandId = dewalt,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 2800,
                    SalesPrice = 3490,
                    MRP = 4100,
                    GSTPercentage = 18,
                    OpeningStock = 20,
                    CurrentStock = 20,
                    MinimumStock = 4,
                    ReorderLevel = 8,
                    Description = "720W heavy-duty motor, advanced dust-sealed slide switch, and optimized airflow cooling channels.",
                    ImagePath = "https://images.unsplash.com/photo-1616401784845-180882ba9ba8?auto=format&fit=crop&w=800&q=80"
                },
                // Rotary Hammers
                new Product
                {
                    ProductCode = "VMR-RH001",
                    ProductName = "Bosch GBH 2-20 DRE Rotary Hammer",
                    CategoryId = hammersCat,
                    BrandId = bosch,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 6800,
                    SalesPrice = 8450,
                    MRP = 9500,
                    GSTPercentage = 18,
                    OpeningStock = 10,
                    CurrentStock = 10,
                    MinimumStock = 2,
                    ReorderLevel = 4,
                    Description = "Fast drilling rate and 30% higher chiseling performance than other rotary hammers in the entry-level class.",
                    ImagePath = "https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&w=800&q=80"
                },
                new Product
                {
                    ProductCode = "VMR-RH002",
                    ProductName = "Makita HR2470 24mm Rotary Hammer",
                    CategoryId = hammersCat,
                    BrandId = makita,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 7500,
                    SalesPrice = 9250,
                    MRP = 10500,
                    GSTPercentage = 18,
                    OpeningStock = 8,
                    CurrentStock = 8,
                    MinimumStock = 2,
                    ReorderLevel = 3,
                    Description = "Versatile 3-mode operation: Rotation only, hammering with rotation, or hammering only for multiple construction applications.",
                    ImagePath = "https://images.unsplash.com/photo-1542282088-fe8426682b8f?auto=format&fit=crop&w=800&q=80"
                },
                // Cut-off Machines
                new Product
                {
                    ProductCode = "VMR-CO001",
                    ProductName = "DeWalt D28730 14-Inch Cut-Off Saw",
                    CategoryId = cutoffCat,
                    BrandId = dewalt,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 9500,
                    SalesPrice = 11890,
                    MRP = 13500,
                    GSTPercentage = 18,
                    OpeningStock = 6,
                    CurrentStock = 6,
                    MinimumStock = 1,
                    ReorderLevel = 2,
                    Description = "2300W motor provides overload protection. Ergonomically designed horizontal D-handle reduces user fatigue.",
                    ImagePath = "https://images.unsplash.com/photo-1534224039826-c7a0dea0e66a?auto=format&fit=crop&w=800&q=80"
                },
                // Circular Saws
                new Product
                {
                    ProductCode = "VMR-CS001",
                    ProductName = "Bosch GKS 190 Professional Circular Saw",
                    CategoryId = sawsCat,
                    BrandId = bosch,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 6100,
                    SalesPrice = 7850,
                    MRP = 8900,
                    GSTPercentage = 18,
                    OpeningStock = 8,
                    CurrentStock = 8,
                    MinimumStock = 2,
                    ReorderLevel = 3,
                    Description = "With 1400W, it has the highest motor power in its class for fast sawing progress in soft and hard wood.",
                    ImagePath = "https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=800&q=80"
                },
                // Welding Machines
                new Product
                {
                    ProductCode = "VMR-WD001",
                    ProductName = "VMR Inverter Welding Machine Arc 200A",
                    CategoryId = weldingCat,
                    BrandId = vmr,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 4900,
                    SalesPrice = 6750,
                    MRP = 7990,
                    GSTPercentage = 18,
                    OpeningStock = 15,
                    CurrentStock = 15,
                    MinimumStock = 3,
                    ReorderLevel = 5,
                    Description = "Advanced IGBT inverter technology with high duty cycle. Energy saving, lightweight, and stable arc output.",
                    ImagePath = "https://images.unsplash.com/photo-1504917595217-d4dc5ebe6122?auto=format&fit=crop&w=800&q=80"
                },
                // Air Compressors
                new Product
                {
                    ProductCode = "VMR-AC001",
                    ProductName = "VMR Air Compressor 3HP 50L Tank",
                    CategoryId = compressorsCat,
                    BrandId = vmr,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 11200,
                    SalesPrice = 14500,
                    MRP = 16800,
                    GSTPercentage = 18,
                    OpeningStock = 5,
                    CurrentStock = 5,
                    MinimumStock = 1,
                    ReorderLevel = 2,
                    Description = "Heavy duty cast iron pump and 3HP copper-winding motor. Ideal for pneumatic tools and painting sprays.",
                    ImagePath = "https://images.unsplash.com/photo-1595206133361-b1fe343e5e23?auto=format&fit=crop&w=800&q=80"
                },
                // Pressure Washers
                new Product
                {
                    ProductCode = "VMR-PW001",
                    ProductName = "VMR High Pressure Washer 1400W",
                    CategoryId = washersCat,
                    BrandId = vmr,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 4100,
                    SalesPrice = 5490,
                    MRP = 6800,
                    GSTPercentage = 18,
                    OpeningStock = 18,
                    CurrentStock = 18,
                    MinimumStock = 3,
                    ReorderLevel = 5,
                    Description = "Delivers up to 110 Bar pressure with auto-stop function to conserve pump lifetime and energy.",
                    ImagePath = "https://images.unsplash.com/photo-1607860108855-64acf2078ed9?auto=format&fit=crop&w=800&q=80"
                },
                // Tool Accessories
                new Product
                {
                    ProductCode = "VMR-TA001",
                    ProductName = "Bosch 26-Piece Screwdriver & Drill Bit Set",
                    CategoryId = accessoriesCat,
                    BrandId = bosch,
                    UnitId = unitSet,
                    WarehouseId = warehouseId,
                    PurchasePrice = 980,
                    SalesPrice = 1450,
                    MRP = 1850,
                    GSTPercentage = 18,
                    OpeningStock = 50,
                    CurrentStock = 50,
                    MinimumStock = 10,
                    ReorderLevel = 15,
                    Description = "Universal accessories set for various drilling and screwdriving jobs, securely stored in a handy carrying case.",
                    ImagePath = "https://images.unsplash.com/photo-1530124566582-ab0510492f27?auto=format&fit=crop&w=800&q=80"
                },
                new Product
                {
                    ProductCode = "VMR-TA002",
                    ProductName = "VMR Heavy Duty Tool Backpack",
                    CategoryId = accessoriesCat,
                    BrandId = vmr,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 1450,
                    SalesPrice = 2150,
                    MRP = 2990,
                    GSTPercentage = 18,
                    OpeningStock = 20,
                    CurrentStock = 20,
                    MinimumStock = 4,
                    ReorderLevel = 6,
                    Description = "Made of durable 1680D ballistic polyester with 38 pockets and a molded hard bottom for tool protection.",
                    ImagePath = "https://images.unsplash.com/photo-1586864387967-d02ef85d93e8?auto=format&fit=crop&w=800&q=80"
                },
                // Safety Equipment
                new Product
                {
                    ProductCode = "VMR-SE001",
                    ProductName = "VMR Industrial Hard Hat (Yellow)",
                    CategoryId = safetyCat,
                    BrandId = vmr,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 240,
                    SalesPrice = 380,
                    MRP = 490,
                    GSTPercentage = 18,
                    OpeningStock = 100,
                    CurrentStock = 100,
                    MinimumStock = 15,
                    ReorderLevel = 25,
                    Description = "High density polyethylene shell with 6-point suspension harness for ultimate impact resistance and shell cooling.",
                    ImagePath = "https://images.unsplash.com/photo-1589793907316-f94015546115?auto=format&fit=crop&w=800&q=80"
                },
                new Product
                {
                    ProductCode = "VMR-SE002",
                    ProductName = "VMR Anti-Scratch Safety Glasses",
                    CategoryId = safetyCat,
                    BrandId = vmr,
                    UnitId = unitPcs,
                    WarehouseId = warehouseId,
                    PurchasePrice = 110,
                    SalesPrice = 180,
                    MRP = 250,
                    GSTPercentage = 18,
                    OpeningStock = 150,
                    CurrentStock = 150,
                    MinimumStock = 20,
                    ReorderLevel = 40,
                    Description = "Clear polycarbonate lenses with anti-scratch and anti-fog coatings. Fully certified to ANSI Z87.1 standards.",
                    ImagePath = "https://images.unsplash.com/photo-1590779033100-9f60a05a013d?auto=format&fit=crop&w=800&q=80"
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

        // Correct any existing negative stock values to 0
        var negativeStockProducts = context.Products.Where(p => p.CurrentStock < 0).ToList();
        if (negativeStockProducts.Any())
        {
            foreach (var p in negativeStockProducts)
            {
                p.CurrentStock = 0;
            }
            await context.SaveChangesAsync();
        }

        // 11. Default Screen Permissions
        if (adminUser != null && !context.ScreenPermissions.Any(sp => sp.UserId == adminUser.Id))
        {
            var defaultPermissions = new List<ScreenPermission>();
            var allScreens = new[]
            {
                // Masters
                "Company Master", "Customer Master", "Supplier Master", "Product Master", "Category Master", "Brand Master", "Unit Master", "Warehouse Master", "Ledger Master", "Employee Master", "Account Groups", "Bank Master", "Tax Settings", "Payment Modes",
                // Sales
                "Quotation", "Sales Order", "Delivery Challan", "Sales Invoice", "Sales Return",
                // Purchase
                "Purchase Order", "Goods Receipt Note", "Purchase Invoice", "Purchase Return",
                // Inventory
                "Stock Opening", "Stock Transfer", "Stock Adjustment", "Physical Stock",
                // Accounts
                "Receipt Voucher", "Payment Voucher", "Contra Voucher", "Journal Voucher", "Debit Note", "Credit Note", "Cash Book", "Bank Book",
                // CRM
                "Leads", "Follow Ups", "Opportunities", "Pipeline View",
                // Reports
                "Sales Reports", "Purchase Reports", "Inventory Reports", "Accounting Reports",
                // Settings
                "User Management", "Role Configuration", "Company Setup", "System Settings"
            };

            foreach (var screen in allScreens)
            {
                defaultPermissions.Add(new ScreenPermission
                {
                    UserId = adminUser.Id,
                    ScreenName = screen,
                    CanView = true,
                    CanEdit = true,
                    CanDelete = true
                });
            }

            context.ScreenPermissions.AddRange(defaultPermissions);
            await context.SaveChangesAsync();
        }
    }
}
