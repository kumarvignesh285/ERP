using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Filters;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize]
[Permission("Masters", "View")]
[Route("Masters")]
public class MastersController : Controller
{
    private readonly IMasterService _masterService;

    public MastersController(IMasterService masterService)
    {
        _masterService = masterService;
    }

    // --- Company ---
    [HttpGet("Company")]
    public async Task<IActionResult> Company()
    {
        var company = await _masterService.GetCompanyAsync() ?? new Company();
        return View(company);
    }

    [HttpPost("Company")]
    public async Task<IActionResult> SaveCompany(Company company)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SaveCompanyAsync(company);
            TempData["Success"] = "Company details saved successfully.";
            return RedirectToAction(nameof(Company));
        }
        return View(company);
    }

    // --- Customer ---
    [HttpGet("Customer")]
    public async Task<IActionResult> Customer()
    {
        var list = await _masterService.GetCustomersAsync();
        return View(list);
    }

    [HttpPost("SaveCustomer")]
    public async Task<IActionResult> SaveCustomer(Customer customer)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SaveCustomerAsync(customer);
            TempData["Success"] = "Customer saved successfully.";
        }
        else
        {
            TempData["Error"] = "Failed to save customer. Check inputs.";
        }
        return RedirectToAction(nameof(Customer));
    }

    [HttpPost("DeleteCustomer")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        await _masterService.DeleteCustomerAsync(id);
        return Json(new { success = true });
    }

    // --- Supplier ---
    [HttpGet("Supplier")]
    public async Task<IActionResult> Supplier()
    {
        var list = await _masterService.GetSuppliersAsync();
        return View(list);
    }

    [HttpPost("SaveSupplier")]
    public async Task<IActionResult> SaveSupplier(Supplier supplier)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SaveSupplierAsync(supplier);
            TempData["Success"] = "Supplier saved successfully.";
        }
        else
        {
            TempData["Error"] = "Failed to save supplier.";
        }
        return RedirectToAction(nameof(Supplier));
    }

    [HttpPost("DeleteSupplier")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        await _masterService.DeleteSupplierAsync(id);
        return Json(new { success = true });
    }

    // --- Product ---
    [HttpGet("Product")]
    public async Task<IActionResult> Product()
    {
        var list = await _masterService.GetProductsAsync();
        var categories = await _masterService.GetCategoriesAsync();
        var brands = await _masterService.GetBrandsAsync();
        var units = await _masterService.GetUnitsAsync();
        var warehouses = await _masterService.GetWarehousesAsync();
        var model = new ProductPageViewModel
        {
            Products = list,
            Categories = categories,
            Brands = brands,
            Units = units,
            Warehouses = warehouses,
            ProductEditLookups = list.Select(p => new ProductEditLookupViewModel
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                CategoryId = p.CategoryId ?? categories.FirstOrDefault()?.Id,
                BrandId = p.BrandId ?? brands.FirstOrDefault()?.Id,
                UnitId = p.UnitId ?? units.FirstOrDefault()?.Id,
                WarehouseId = p.WarehouseId ?? warehouses.FirstOrDefault()?.Id,
                PurchasePrice = p.PurchasePrice,
                SalesPrice = p.SalesPrice,
                MRP = p.MRP,
                GSTPercentage = p.GSTPercentage,
                MinimumStock = p.MinimumStock,
                ReorderLevel = p.ReorderLevel,
                Description = p.Description,
                DocumentPath = p.DocumentPath
            }).ToList()
        };
        return View(model);
    }

    [HttpPost("SaveProduct")]
    public async Task<IActionResult> SaveProduct(Product product, IFormFile? pdfDocument)
    {
        if (pdfDocument != null && pdfDocument.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents", "Products");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + pdfDocument.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await pdfDocument.CopyToAsync(fileStream);
            }
            product.DocumentPath = "/uploads/documents/Products/" + uniqueFileName;
        }

        if (ModelState.IsValid)
        {
            await _masterService.SaveProductAsync(product);
            TempData["Success"] = "Product saved successfully.";
        }
        else
        {
            TempData["Error"] = "Failed to save product.";
        }
        return RedirectToAction(nameof(Product));
    }

    [HttpPost("DeleteProduct")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await _masterService.DeleteProductAsync(id);
        return Json(new { success = true });
    }

    [HttpGet("DownloadProductTemplate")]
    public IActionResult DownloadProductTemplate()
    {
        var templateData = new[]
        {
            new
            {
                ProductCode = "PROD001",
                ProductName = "Chainsaw Model X",
                Category = "Chainsaw",
                Brand = "BrandA",
                Unit = "Pcs",
                Warehouse = "Main Warehouse",
                HSNCode = "8467",
                Barcode = "123456789",
                PurchasePrice = 12000.00,
                SalesPrice = 15000.00,
                MRP = 16000.00,
                Discount = 500.00,
                GSTPercentage = 18.00,
                OpeningStock = 10,
                MinimumStock = 2,
                MaximumStock = 20,
                ReorderLevel = 5,
                Description = "Heavy duty chainsaw"
            },
            new
            {
                ProductCode = "PROD002",
                ProductName = "Powertool Drill Y",
                Category = "Powertool",
                Brand = "BrandB",
                Unit = "Pcs",
                Warehouse = "Main Warehouse",
                HSNCode = "8459",
                Barcode = "987654321",
                PurchasePrice = 3500.00,
                SalesPrice = 4500.00,
                MRP = 5000.00,
                Discount = 200.00,
                GSTPercentage = 12.00,
                OpeningStock = 25,
                MinimumStock = 5,
                MaximumStock = 50,
                ReorderLevel = 10,
                Description = "12V cordless drill"
            }
        };

        var memoryStream = new System.IO.MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(memoryStream, templateData);
        memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
        
        return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Product_Bulk_Upload_Template.xlsx");
    }

    [HttpPost("BulkUploadProducts")]
    public async Task<IActionResult> BulkUploadProducts(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "Please select a valid Excel file." });
        }

        var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".xlsx" or ".xls"))
        {
            return Json(new { success = false, message = "Only Excel files (.xlsx, .xls) are supported." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var (successCount, errors) = await _masterService.BulkUploadProductsAsync(stream);

            if (errors.Any())
            {
                return Json(new { 
                    success = false, 
                    message = $"{successCount} products uploaded, but there were some errors.", 
                    errors = errors 
                });
            }

            return Json(new { success = true, message = $"Successfully uploaded {successCount} products." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred: " + ex.Message });
        }
    }

    [HttpPost("PreviewImport")]
    public async Task<IActionResult> PreviewImport(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "Please select a valid Excel or PDF file." });
        }

        var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".xlsx" or ".xls" or ".pdf"))
        {
            return Json(new { success = false, message = "Only Excel (.xlsx, .xls) and PDF (.pdf) files are supported." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var items = await _masterService.PreviewImportAsync(stream, ext);
            
            var categories = await _masterService.GetCategoriesAsync();
            var brands = await _masterService.GetBrandsAsync();
            var units = await _masterService.GetUnitsAsync();
            var warehouses = await _masterService.GetWarehousesAsync();
            
            return Json(new { 
                success = true, 
                items = items,
                categories = categories.Select(c => c.CategoryName).ToList(),
                brands = brands.Select(b => b.BrandName).ToList(),
                units = units.Select(u => u.UnitSymbol).ToList(),
                warehouses = warehouses.Select(w => w.WarehouseName).ToList()
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred during parsing: " + ex.Message });
        }
    }

    [HttpPost("CommitImport")]
    public async Task<IActionResult> CommitImport([FromBody] List<ImportProductCommitDto> items)
    {
        if (items == null || !items.Any())
        {
            return Json(new { success = false, message = "No products provided for import." });
        }

        try
        {
            var list = await _masterService.CommitImportAsync(items);
            return Json(new { success = true, items = list, message = $"Successfully imported/updated {list.Count} products." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred during import: " + ex.Message });
        }
    }

    // --- Category ---
    [HttpGet("Category")]
    public async Task<IActionResult> Category()
    {
        var list = await _masterService.GetCategoriesAsync();
        return View(list);
    }

    [HttpPost("SaveCategory")]
    public async Task<IActionResult> SaveCategory(Category category)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SaveCategoryAsync(category);
            TempData["Success"] = "Category saved successfully.";
        }
        return RedirectToAction(nameof(Category));
    }

    [HttpPost("DeleteCategory")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await _masterService.DeleteCategoryAsync(id);
        return Json(new { success = true });
    }

    // --- Brand ---
    [HttpGet("Brand")]
    public async Task<IActionResult> Brand()
    {
        var list = await _masterService.GetBrandsAsync();
        return View(list);
    }

    [HttpPost("SaveBrand")]
    public async Task<IActionResult> SaveBrand(Brand brand)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SaveBrandAsync(brand);
            TempData["Success"] = "Brand saved successfully.";
        }
        return RedirectToAction(nameof(Brand));
    }

    [HttpPost("DeleteBrand")]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        await _masterService.DeleteBrandAsync(id);
        return Json(new { success = true });
    }

    // --- Unit ---
    [HttpGet("Unit")]
    public async Task<IActionResult> Unit()
    {
        var list = await _masterService.GetUnitsAsync();
        return View(list);
    }

    [HttpPost("SaveUnit")]
    public async Task<IActionResult> SaveUnit(Unit unit)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SaveUnitAsync(unit);
            TempData["Success"] = "Unit saved successfully.";
        }
        return RedirectToAction(nameof(Unit));
    }

    [HttpPost("DeleteUnit")]
    public async Task<IActionResult> DeleteUnit(int id)
    {
        await _masterService.DeleteUnitAsync(id);
        return Json(new { success = true });
    }

    // --- Warehouse ---
    [HttpGet("Warehouse")]
    public async Task<IActionResult> Warehouse()
    {
        var list = await _masterService.GetWarehousesAsync();
        return View(list);
    }

    [HttpPost("SaveWarehouse")]
    public async Task<IActionResult> SaveWarehouse(Warehouse warehouse)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SaveWarehouseAsync(warehouse);
            TempData["Success"] = "Warehouse saved successfully.";
        }
        return RedirectToAction(nameof(Warehouse));
    }

    [HttpPost("DeleteWarehouse")]
    public async Task<IActionResult> DeleteWarehouse(int id)
    {
        await _masterService.DeleteWarehouseAsync(id);
        return Json(new { success = true });
    }

    // --- Employee ---
    [HttpGet("Employee")]
    public async Task<IActionResult> Employee()
    {
        var list = await _masterService.GetEmployeesAsync();
        return View(list);
    }

    [HttpPost("SaveEmployee")]
    public async Task<IActionResult> SaveEmployee(Employee employee)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SaveEmployeeAsync(employee);
            TempData["Success"] = "Employee saved successfully.";
        }
        return RedirectToAction(nameof(Employee));
    }

    [HttpPost("DeleteEmployee")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        await _masterService.DeleteEmployeeAsync(id);
        return Json(new { success = true });
    }

    // --- Account Group ---
    [HttpGet("AccountGroup")]
    public async Task<IActionResult> AccountGroup()
    {
        var list = await _masterService.GetAccountGroupsAsync();
        return View(list);
    }

    [HttpPost("SaveAccountGroup")]
    public async Task<IActionResult> SaveAccountGroup(AccountGroup group)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SaveAccountGroupAsync(group);
            TempData["Success"] = "Account Group saved successfully.";
        }
        return RedirectToAction(nameof(AccountGroup));
    }

    [HttpPost("DeleteAccountGroup")]
    public async Task<IActionResult> DeleteAccountGroup(int id)
    {
        await _masterService.DeleteAccountGroupAsync(id);
        return Json(new { success = true });
    }

    // --- Ledger ---
    [HttpGet("Ledger")]
    public async Task<IActionResult> Ledger()
    {
        return View(new LedgerPageViewModel
        {
            AccountGroups = await _masterService.GetAccountGroupsAsync(),
            Ledgers = await _masterService.GetLedgersAsync()
        });
    }

    [HttpPost("SaveLedger")]
    public async Task<IActionResult> SaveLedger(Ledger ledger)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SaveLedgerAsync(ledger);
            TempData["Success"] = "Ledger saved successfully.";
        }
        return RedirectToAction(nameof(Ledger));
    }

    [HttpPost("DeleteLedger")]
    public async Task<IActionResult> DeleteLedger(int id)
    {
        await _masterService.DeleteLedgerAsync(id);
        return Json(new { success = true });
    }

    // --- Bank ---
    [HttpGet("Bank")]
    public async Task<IActionResult> Bank()
    {
        return View(new BankPageViewModel
        {
            Ledgers = await _masterService.GetLedgersAsync(),
            Banks = await _masterService.GetBanksAsync()
        });
    }

    [HttpPost("SaveBank")]
    public async Task<IActionResult> SaveBank(Bank bank)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SaveBankAsync(bank);
            TempData["Success"] = "Bank saved successfully.";
        }
        return RedirectToAction(nameof(Bank));
    }

    [HttpPost("DeleteBank")]
    public async Task<IActionResult> DeleteBank(int id)
    {
        await _masterService.DeleteBankAsync(id);
        return Json(new { success = true });
    }

    // --- Tax ---
    [HttpGet("Tax")]
    public async Task<IActionResult> Tax()
    {
        var list = await _masterService.GetTaxesAsync();
        return View(list);
    }

    [HttpPost("SaveTax")]
    public async Task<IActionResult> SaveTax(Tax tax)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SaveTaxAsync(tax);
            TempData["Success"] = "Tax settings saved successfully.";
        }
        return RedirectToAction(nameof(Tax));
    }

    [HttpPost("DeleteTax")]
    public async Task<IActionResult> DeleteTax(int id)
    {
        await _masterService.DeleteTaxAsync(id);
        return Json(new { success = true });
    }

    // --- Payment Mode ---
    [HttpGet("PaymentMode")]
    public async Task<IActionResult> PaymentMode()
    {
        var list = await _masterService.GetPaymentModesAsync();
        return View(list);
    }

    [HttpPost("SavePaymentMode")]
    public async Task<IActionResult> SavePaymentMode(PaymentMode mode)
    {
        if (ModelState.IsValid)
        {
            await _masterService.SavePaymentModeAsync(mode);
            TempData["Success"] = "Payment Mode saved successfully.";
        }
        return RedirectToAction(nameof(PaymentMode));
    }

    [HttpPost("DeletePaymentMode")]
    public async Task<IActionResult> DeletePaymentMode(int id)
    {
        await _masterService.DeletePaymentModeAsync(id);
        return Json(new { success = true });
    }
}
