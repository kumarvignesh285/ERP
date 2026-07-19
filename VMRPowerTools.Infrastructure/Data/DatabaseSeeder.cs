using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task ReseedAsync(WebsiteDbContext context)
    {
        // Check if database is already seeded with the WhatsApp product images
        var hasWhatsAppProducts = await context.Products.AnyAsync(p => p.ImagePath != null && p.ImagePath.Contains("WhatsApp"));
        if (false && hasWhatsAppProducts)
        {
            return; // Already seeded
        }

        // Clean tables to replace any legacy temporary seed items (like Chain Saws) with real ERP products
        try
        {
            // Clear transactional tables
            context.StockTransactions.RemoveRange(context.StockTransactions);
            context.SalesInvoiceItems.RemoveRange(context.SalesInvoiceItems);
            context.SalesInvoices.RemoveRange(context.SalesInvoices);
            context.SalesOrderItems.RemoveRange(context.SalesOrderItems);
            context.SalesOrders.RemoveRange(context.SalesOrders);
            
            // Clear catalogs
            context.Products.RemoveRange(context.Products);
            context.Categories.RemoveRange(context.Categories);
            context.Brands.RemoveRange(context.Brands);

            await context.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Fallback raw SQL cleanup ignoring foreign key constraints
            var sql = @"
                EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT all';
                DELETE FROM [StockTransactions];
                DELETE FROM [SalesInvoiceItems];
                DELETE FROM [SalesInvoices];
                DELETE FROM [SalesOrderItems];
                DELETE FROM [SalesOrders];
                DELETE FROM [Products];
                DELETE FROM [Categories];
                DELETE FROM [Brands];
                EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all';
            ";
            await context.Database.ExecuteSqlRawAsync(sql);
        }

        // 1. Seed Original ERP Brands
        var brands = new List<Brand>
        {
            new Brand { BrandName = "DeWalt" },
            new Brand { BrandName = "Bosch" },
            new Brand { BrandName = "Makita" },
            new Brand { BrandName = "Hitachi" },
            new Brand { BrandName = "Metabo" },
            new Brand { BrandName = "VMR" }
        };
        context.Brands.AddRange(brands);
        await context.SaveChangesAsync();

        // 2. Seed Original ERP Categories
        var categories = new List<Category>
        {
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
        };
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        // Fetch saved models to reference IDs
        var dbBrands = await context.Brands.ToDictionaryAsync(b => b.BrandName, b => b.Id);
        var dbCats = await context.Categories.ToDictionaryAsync(c => c.CategoryName, c => c.Id);

        // 3. Seed Original ERP Products with Premium Generated Local Images
        var products = new List<Product>
        {
            // Power Drills
            new Product
            {
                ProductCode = "VMR-DR001",
                ProductName = "DeWalt DCD771C2 20V Max Cordless Drill",
                CategoryId = dbCats["Power Drills"],
                BrandId = dbBrands["DeWalt"],
                PurchasePrice = 6200,
                SalesPrice = 7490,
                MRP = 8500,
                GSTPercentage = 18,
                OpeningStock = 15,
                CurrentStock = 15,
                MinimumStock = 3,
                ReorderLevel = 5,
                Description = "High performance motor delivers 300 unit watts out (UWO) of power ability completing a wide range of applications.",
                ImagePath = "/images/products/product_power_drill.jpg"
            },
            new Product
            {
                ProductCode = "VMR-DR002",
                ProductName = "Bosch GSB 501 500W Impact Drill",
                CategoryId = dbCats["Power Drills"],
                BrandId = dbBrands["Bosch"],
                PurchasePrice = 2900,
                SalesPrice = 3890,
                MRP = 4500,
                GSTPercentage = 18,
                OpeningStock = 25,
                CurrentStock = 25,
                MinimumStock = 5,
                ReorderLevel = 10,
                Description = "Powerful and reliable tool with a compact design. Ergonomic handle design makes it comfortable for overhead work.",
                ImagePath = "/images/products/product_power_drill.jpg"
            },
            new Product
            {
                ProductCode = "VMR-DR003",
                ProductName = "Makita HP1630 16mm Hammer Drill",
                CategoryId = dbCats["Power Drills"],
                BrandId = dbBrands["Makita"],
                PurchasePrice = 4600,
                SalesPrice = 5650,
                MRP = 6200,
                GSTPercentage = 18,
                OpeningStock = 12,
                CurrentStock = 12,
                MinimumStock = 2,
                ReorderLevel = 4,
                Description = "Cylinder-like motor housing and aluminum gear housing cover provide high durability and extended tool lifespan.",
                ImagePath = "/images/products/product_power_drill.jpg"
            },

            // Angle Grinders
            new Product
            {
                ProductCode = "VMR-GR001",
                ProductName = "Bosch GWS 600 Professional Angle Grinder",
                CategoryId = dbCats["Angle Grinders"],
                BrandId = dbBrands["Bosch"],
                PurchasePrice = 2500,
                SalesPrice = 3250,
                MRP = 3900,
                GSTPercentage = 18,
                OpeningStock = 30,
                CurrentStock = 30,
                MinimumStock = 6,
                ReorderLevel = 10,
                Description = "670W maximum input power with bullet-proof guard for high-level user protection during metal cutting and grinding.",
                ImagePath = "/images/products/product_angle_grinder.jpg"
            },
            new Product
            {
                ProductCode = "VMR-GR002",
                ProductName = "DeWalt DWE4010 4-Inch Angle Grinder",
                CategoryId = dbCats["Angle Grinders"],
                BrandId = dbBrands["DeWalt"],
                PurchasePrice = 2800,
                SalesPrice = 3490,
                MRP = 4100,
                GSTPercentage = 18,
                OpeningStock = 20,
                CurrentStock = 20,
                MinimumStock = 4,
                ReorderLevel = 8,
                Description = "720W heavy-duty motor, advanced dust-sealed slide switch, and optimized airflow cooling channels.",
                ImagePath = "/images/products/product_angle_grinder.jpg"
            },

            // Rotary Hammers
            new Product
            {
                ProductCode = "VMR-RH001",
                ProductName = "Bosch GBH 2-20 DRE Rotary Hammer",
                CategoryId = dbCats["Rotary Hammers"],
                BrandId = dbBrands["Bosch"],
                PurchasePrice = 6800,
                SalesPrice = 8450,
                MRP = 9500,
                GSTPercentage = 18,
                OpeningStock = 10,
                CurrentStock = 10,
                MinimumStock = 2,
                ReorderLevel = 4,
                Description = "Fast drilling rate and 30% higher chiseling performance than other rotary hammers in the entry-level class.",
                ImagePath = "/images/products/product_rotary_hammer.jpg"
            },
            new Product
            {
                ProductCode = "VMR-RH002",
                ProductName = "Makita HR2470 24mm Rotary Hammer",
                CategoryId = dbCats["Rotary Hammers"],
                BrandId = dbBrands["Makita"],
                PurchasePrice = 7500,
                SalesPrice = 9250,
                MRP = 10500,
                GSTPercentage = 18,
                OpeningStock = 8,
                CurrentStock = 8,
                MinimumStock = 2,
                ReorderLevel = 3,
                Description = "Versatile 3-mode operation: Rotation only, hammering with rotation, or hammering only for multiple construction applications.",
                ImagePath = "/images/products/product_rotary_hammer.jpg"
            },

            // Cut-off Machines
            new Product
            {
                ProductCode = "VMR-CO001",
                ProductName = "DeWalt D28730 14-Inch Cut-Off Saw",
                CategoryId = dbCats["Cut-off Machines"],
                BrandId = dbBrands["DeWalt"],
                PurchasePrice = 9500,
                SalesPrice = 11890,
                MRP = 13500,
                GSTPercentage = 18,
                OpeningStock = 6,
                CurrentStock = 6,
                MinimumStock = 1,
                ReorderLevel = 2,
                Description = "2300W motor provides overload protection. Ergonomically designed horizontal D-handle reduces user fatigue.",
                ImagePath = "/images/products/product_cutoff_machine.jpg"
            },

            // Circular Saws
            new Product
            {
                ProductCode = "VMR-CS001",
                ProductName = "Bosch GKS 190 Professional Circular Saw",
                CategoryId = dbCats["Circular Saws"],
                BrandId = dbBrands["Bosch"],
                PurchasePrice = 6100,
                SalesPrice = 7850,
                MRP = 8900,
                GSTPercentage = 18,
                OpeningStock = 8,
                CurrentStock = 8,
                MinimumStock = 2,
                ReorderLevel = 3,
                Description = "With 1400W, it has the highest motor power in its class for fast sawing progress in soft and hard wood.",
                ImagePath = "/images/products/product_circular_saw.jpg"
            },

            // Welding Machines
            new Product
            {
                ProductCode = "VMR-WD001",
                ProductName = "VMR Inverter Welding Machine Arc 200A",
                CategoryId = dbCats["Welding Machines"],
                BrandId = dbBrands["VMR"],
                PurchasePrice = 4900,
                SalesPrice = 6750,
                MRP = 7990,
                GSTPercentage = 18,
                OpeningStock = 15,
                CurrentStock = 15,
                MinimumStock = 3,
                ReorderLevel = 5,
                Description = "Advanced IGBT inverter technology with high duty cycle. Energy saving, lightweight, and stable arc output.",
                ImagePath = "/images/products/product_welding_machine.jpg"
            },

            // Air Compressors
            new Product
            {
                ProductCode = "VMR-AC001",
                ProductName = "VMR Air Compressor 3HP 50L Tank",
                CategoryId = dbCats["Air Compressors"],
                BrandId = dbBrands["VMR"],
                PurchasePrice = 11200,
                SalesPrice = 14500,
                MRP = 16800,
                GSTPercentage = 18,
                OpeningStock = 5,
                CurrentStock = 5,
                MinimumStock = 1,
                ReorderLevel = 2,
                Description = "Heavy duty cast iron pump and 3HP copper-winding motor. Ideal for pneumatic tools and painting sprays.",
                ImagePath = "/images/products/product_air_compressor.jpg"
            },

            // Pressure Washers
            new Product
            {
                ProductCode = "VMR-PW001",
                ProductName = "VMR High Pressure Washer 1400W",
                CategoryId = dbCats["Pressure Washers"],
                BrandId = dbBrands["VMR"],
                PurchasePrice = 4100,
                SalesPrice = 5490,
                MRP = 6800,
                GSTPercentage = 18,
                OpeningStock = 18,
                CurrentStock = 18,
                MinimumStock = 3,
                ReorderLevel = 5,
                Description = "Delivers up to 110 Bar pressure with auto-stop function to conserve pump lifetime and energy.",
                ImagePath = "/images/products/product_pressure_washer.jpg"
            },

            // Tool Accessories
            new Product
            {
                ProductCode = "VMR-TA001",
                ProductName = "Bosch 26-Piece Screwdriver & Drill Bit Set",
                CategoryId = dbCats["Tool Accessories"],
                BrandId = dbBrands["Bosch"],
                PurchasePrice = 980,
                SalesPrice = 1450,
                MRP = 1850,
                GSTPercentage = 18,
                OpeningStock = 50,
                CurrentStock = 50,
                MinimumStock = 10,
                ReorderLevel = 15,
                Description = "Universal accessories set for various drilling and screwdriving jobs, securely stored in a handy carrying case.",
                ImagePath = "/images/products/no_image_placeholder.jpg"
            },
            new Product
            {
                ProductCode = "VMR-TA002",
                ProductName = "VMR Heavy Duty Tool Backpack",
                CategoryId = dbCats["Tool Accessories"],
                BrandId = dbBrands["VMR"],
                PurchasePrice = 1450,
                SalesPrice = 2150,
                MRP = 2990,
                GSTPercentage = 18,
                OpeningStock = 20,
                CurrentStock = 20,
                MinimumStock = 4,
                ReorderLevel = 6,
                Description = "Made of durable 1680D ballistic polyester with 38 pockets and a molded hard bottom for tool protection.",
                ImagePath = "/images/products/no_image_placeholder.jpg"
            },

            // Safety Equipment
            new Product
            {
                ProductCode = "VMR-SE001",
                ProductName = "VMR Industrial Hard Hat (Yellow)",
                CategoryId = dbCats["Safety Equipment"],
                BrandId = dbBrands["VMR"],
                PurchasePrice = 240,
                SalesPrice = 380,
                MRP = 490,
                GSTPercentage = 18,
                OpeningStock = 100,
                CurrentStock = 100,
                MinimumStock = 15,
                ReorderLevel = 25,
                Description = "High density polyethylene shell with 6-point suspension harness for ultimate impact resistance and shell cooling.",
                ImagePath = "/images/products/product_concrete_cutter.jpg"
            },
            new Product
            {
                ProductCode = "VMR-SE002",
                ProductName = "VMR Anti-Scratch Safety Glasses",
                CategoryId = dbCats["Safety Equipment"],
                BrandId = dbBrands["VMR"],
                PurchasePrice = 110,
                SalesPrice = 180,
                MRP = 250,
                GSTPercentage = 18,
                OpeningStock = 150,
                CurrentStock = 150,
                MinimumStock = 20,
                ReorderLevel = 40,
                Description = "Clear polycarbonate lenses with anti-scratch and anti-fog coatings. Fully certified to ANSI Z87.1 standards.",
                ImagePath = "/images/products/product_concrete_cutter.jpg"
            }
        };

        // 4. Scan and Seed WhatsApp images dynamically
        var wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (!Directory.Exists(wwwrootPath))
        {
            wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "VMRPowerTools.Website", "wwwroot");
            if (!Directory.Exists(wwwrootPath))
            {
                wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
        }

        var productsDir = Path.Combine(wwwrootPath, "images", "products");
        if (Directory.Exists(productsDir))
        {
            var files = Directory.GetFiles(productsDir, "WhatsApp Image*.jpeg")
                                 .Concat(Directory.GetFiles(productsDir, "WhatsApp Image*.jpg"))
                                 .ToList();

            var uniqueFiles = files.ToList();

            int codeCounter = 100;
            foreach (var file in uniqueFiles)
            {
                var filename = Path.GetFileName(file);
                var imagePath = $"/images/products/{filename}";

                if (products.Any(p => p.ImagePath == imagePath))
                {
                    continue;
                }

                var info = GetWhatsAppProductInfo(filename);
                
                int? categoryId = dbCats.ContainsKey(info.category) ? dbCats[info.category] : dbCats["Circular Saws"];
                int? brandId = dbBrands.ContainsKey(info.brand) ? dbBrands[info.brand] : dbBrands["VMR"];

                products.Add(new Product
                {
                    ProductCode = info.code,
                    ProductName = info.name,
                    CategoryId = categoryId,
                    BrandId = brandId,
                    PurchasePrice = Math.Round(info.price * 0.78m),
                    SalesPrice = info.price,
                    MRP = Math.Round(info.price * 1.15m),
                    GSTPercentage = 18,
                    OpeningStock = 10 + (codeCounter % 15),
                    CurrentStock = 10 + (codeCounter % 15),
                    MinimumStock = 2,
                    ReorderLevel = 4,
                    Description = info.desc,
                    ImagePath = imagePath
                });
                codeCounter++;
            }
        }

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private static (string name, string code, string category, string brand, string desc, decimal price) GetWhatsAppProductInfo(string filename)
    {
        // Define known products based on timestamp segments in their filenames
        if (filename.Contains("22.56.31"))
            return ("Sun-Flower SF-GCS-18 Gasoline Chain Saw 58cc", "SF-GCS-18", "Circular Saws", "VMR", "Sun-Flower SF-GCS-18 gasoline chain saw with 58cc displacement, 2600W power output, and 18-inch guide bar. Features high reliability and durability components.", 18990);
        if (filename.Contains("22.56.34"))
            return ("Powertex PPT-GCS-18 Gasoline Chain Saw 58cc", "PPT-GCS-18", "Circular Saws", "VMR", "Powertex PPT-GCS-18 gasoline chain saw featuring a 58cc engine, 2600W power, and 18-inch guide bar. Ideal for heavy-duty logging and agricultural operations.", 19500);
        if (filename.Contains("22.56.37") && filename.Contains("("))
            return ("Power Distribution Board 8-Way", "WA-DB-08", "Tool Accessories", "VMR", "Heavy duty industrial electrical distribution board with 8 sockets and high-amp overload protection.", 2400);
        if (filename.Contains("22.56.37"))
            return ("Hi-MAX IC-045A Gasoline Chain Saw 45cc", "IC-045A", "Circular Saws", "VMR", "Hi-MAX IC-045A professional gasoline chain saw with 45cc engine and 450mm (18-inch) bar length. Designed for high durability and performance.", 14500);
        if (filename.Contains("22.56.38"))
            return ("Hi-MAX IC-024 10mm Drill Machine 400W", "IC-024", "Power Drills", "Hitachi", "Hi-MAX IC-024 10mm chuck drill machine with 400W motor and 2800 RPM speed. Ergonomic design for comfortable usage.", 3200);
        if (filename.Contains("22.56.39"))
            return ("Hi-MAX IC-058A Gasoline Chain Saw 58cc", "IC-058A", "Circular Saws", "VMR", "Hi-MAX IC-058A gasoline chain saw with 58cc displacement, 2400 Watts power, and 450mm bar length. Features improved quality components.", 16990);
        if (filename.Contains("22.56.40"))
            return ("Powerbilt PBT-CW-2700 Heavy-Duty Car Washer 2700W", "PBT-CW-2700", "Pressure Washers", "VMR", "Powerbilt PBT-CW-2700 heavy duty 2700W car washer with 250 Bar pressure and 100% copper motor. Includes 8-meter high-pressure hose.", 9800);
        if (filename.Contains("22.56.41"))
            return ("Powerbilt PBT-CL-WR21 Cordless Brushless Wrench 21V", "PBT-CL-WR21", "Power Drills", "Hitachi", "Powerbilt PBT-CL-WR21 21V cordless brushless impact wrench with 350 N.m torque and 12.7mm drive. Features LED light and forward/reserve rotation.", 7200);
        if (filename.Contains("22.56.44"))
            return ("Rainbow RAINBOW58-18 Gasoline Chain Saw", "RAINBOW58-18", "Circular Saws", "VMR", "Rainbow RAINBOW58-18 professional 18-inch gasoline chainsaw with easy chain adjuster for proper saw tension. Built for professional garden use.", 15990);
        if (filename.Contains("23.10.47"))
            return ("Husqvarna HP/LS+ 2-Stroke & 4-Stroke Oil Premium", "HUSQ-OIL-01", "Tool Accessories", "VMR", "Husqvarna premium quality HP/LS+ 2-stroke and 10W-40 4-stroke oils and lubricants for optimal power and engine protection.", 850);
        if (filename.Contains("23.11.11"))
            return ("Powerbilt PBT-CL-CS8 Cordless Chain Saw 21V", "PBT-CL-CS8", "Circular Saws", "VMR", "Powerbilt PBT-CL-CS8 21V cordless brushless chain saw with 8-inch guide bar. Lightweight and durable battery operated saw for one-handed operation.", 8900);
        if (filename.Contains("23.11.14"))
            return ("Powerbilt PBT-GCSM-40 12\" Mini Chainsaw 40cc", "PBT-GCSM-40", "Circular Saws", "VMR", "Powerbilt Gasoline Chainsaw (PBT-GCSM-40) 12-inch mini with 40cc engine displacement, 2 stroke, 1 cylinder. Easy handling and Japanese cylinder set.", 7500);
        if (filename.Contains("23.11.15"))
            return ("Powerbilt PBT-GCS-74 Gasoline Chain Saw 74cc", "PBT-GCS-74", "Circular Saws", "VMR", "Powerbilt PBT-GCS-74 Gasoline Chain Saw featuring 74cc displacement, 3.6 KW powerful engine, and magnesium brake assembly.", 21500);
        if (filename.Contains("23.11.53"))
            return ("Powertex X-Treme Gasoline Chain Saw 74cc", "PPT-GCS-74", "Circular Saws", "VMR", "Powertex X-Treme 74cc heavy-duty gasoline chainsaw with high-performance engine for forestry logging.", 22990);
        if (filename.Contains("23.12.19"))
            return ("Orezen 52cc Trolley Brush Cutter", "ORZ-TBC-52", "Circular Saws", "VMR", "Orezen 52cc high-powered trolley brush cutter for tackling tough brush and weeds with ease. Heavy duty wheels for smooth maneuvering.", 16500);
        if (filename.Contains("23.12.22"))
            return ("Husqvarna HH 270MP Multi Purpose Engine 270cc", "HUSQ-HH-270", "Circular Saws", "VMR", "Husqvarna HH 270MP multi-purpose 4-stroke air-cooled gasoline engine with 270cc displacement and 6.5 HP power output.", 18500);

        int hashCode = Math.Abs(filename.GetHashCode());
        
        // Define clean generic name templates that look premium and never mismatch visually
        string[] productTypes = new string[] {
            "Professional Power Tool",
            "Industrial Equipment",
            "Heavy-Duty Spare Part",
            "Premium Machinery Unit",
            "Multi-Purpose Utility Kit",
            "High-Performance Tool Accessory"
        };
        string productType = productTypes[hashCode % productTypes.Length];

        string codeSuffix = (hashCode % 1000).ToString("D3");
        string code = $"WA-{codeSuffix}"; // Clean model code (e.g. WA-123)
        string name = $"{productType}"; // Product name is only the product type itself (no VMR or Bosch prepended in title)
        string desc = $"Professional-grade {productType.ToLower()} designed for high performance, industrial reliability, and durability.";
        decimal price = 2500 + (hashCode % 15) * 1000;

        return (name, code, "Tool Accessories", "VMR", desc, price);
    }
}
