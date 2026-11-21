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

    private bool IsAjaxRequest() =>
        Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
        Request.Headers["Accept"].ToString().Contains("application/json") ||
        Request.ContentType?.Contains("application/json") == true;

    private Dictionary<string, string> GetModelStateErrors() =>
        ModelState.Where(x => x.Value?.Errors.Count > 0)
                  .ToDictionary(
                      k => k.Key,
                      v => v.Value!.Errors.First().ErrorMessage
                  );

    // --- Company ---
    [HttpGet("Company")]
    public async Task<IActionResult> Company()
    {
        var company = await _masterService.GetCompanyAsync() ?? new Company();
        return View(company);
    }

    [HttpPost("Company")]
    public async Task<IActionResult> SaveCompany([FromForm] Company company)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Please correct the validation errors.", GetModelStateErrors()));
            return View(company);
        }

        try
        {
            await _masterService.SaveCompanyAsync(company);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Company details saved successfully.", company));
            TempData["Success"] = "Company details saved successfully.";
            return RedirectToAction(nameof(Company));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return View(company);
        }
    }

    // --- Customer ---
    [HttpGet("Customer")]
    public async Task<IActionResult> Customer()
    {
        var list = await _masterService.GetCustomersAsync();
        return View(new CustomerPageViewModel { Customers = list });
    }

    [HttpPost("SaveCustomer")]
    public async Task<IActionResult> SaveCustomer([FromForm] CustomerInputModel? model, [FromForm] CustomerInputModel? form)
    {
        var input = model?.CustomerCode != null ? model : (form ?? model ?? new CustomerInputModel());
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Please correct the validation errors.", GetModelStateErrors()));
            TempData["Error"] = "Failed to save customer. Check inputs.";
            return RedirectToAction(nameof(Customer));
        }

        try
        {
            var customer = new Customer
            {
                Id = input.Id,
                CustomerCode = input.CustomerCode,
                CustomerName = input.CustomerName,
                ContactPerson = input.ContactPerson,
                MobileNumber = input.MobileNumber,
                Email = input.Email,
                GSTNumber = input.GSTNumber,
                Address = input.Address,
                City = input.City,
                State = input.State,
                CreditLimit = input.CreditLimit,
                OpeningBalance = input.OpeningBalance,
                BalanceType = input.BalanceType,
                IsActive = input.IsActive
            };
            var saved = await _masterService.SaveCustomerAsync(customer);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Customer saved successfully.", saved));
            TempData["Success"] = "Customer saved successfully.";
            return RedirectToAction(nameof(Customer));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Customer));
        }
    }

    [HttpPost("DeleteCustomer")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var (success, msg) = await _masterService.DeleteCustomerAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }

    // --- Supplier ---
    [HttpGet("Supplier")]
    public async Task<IActionResult> Supplier()
    {
        var list = await _masterService.GetSuppliersAsync();
        return View(new SupplierPageViewModel { Suppliers = list });
    }

    [HttpPost("SaveSupplier")]
    public async Task<IActionResult> SaveSupplier([FromForm] SupplierInputModel? model, [FromForm] SupplierInputModel? form)
    {
        var input = model?.SupplierCode != null ? model : (form ?? model ?? new SupplierInputModel());
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Please correct the validation errors.", GetModelStateErrors()));
            TempData["Error"] = "Failed to save supplier. Check inputs.";
            return RedirectToAction(nameof(Supplier));
        }

        try
        {
            var supplier = new Supplier
            {
                Id = input.Id,
                SupplierCode = input.SupplierCode,
                SupplierName = input.SupplierName,
                ContactPerson = input.ContactPerson,
                Mobile = input.Mobile,
                Email = input.Email,
                GSTNumber = input.GSTNumber,
                Address = input.Address,
                City = input.City,
                State = input.State,
                OpeningBalance = input.OpeningBalance,
                BalanceType = input.BalanceType,
                IsActive = input.IsActive
            };
            var saved = await _masterService.SaveSupplierAsync(supplier);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Supplier saved successfully.", saved));
            TempData["Success"] = "Supplier saved successfully.";
            return RedirectToAction(nameof(Supplier));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Supplier));
        }
    }

    [HttpPost("DeleteSupplier")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var (success, msg) = await _masterService.DeleteSupplierAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
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
    public async Task<IActionResult> SaveProduct([FromForm] ProductInputModel? model, [FromForm] ProductInputModel? form, IFormFile? pdfDocument)
    {
        var input = model?.ProductCode != null ? model : (form ?? model ?? new ProductInputModel());
        if (pdfDocument != null && pdfDocument.Length > 0)
        {
            var ext = Path.GetExtension(pdfDocument.FileName).ToLowerInvariant();
            if (ext != ".pdf")
            {
                if (IsAjaxRequest())
                    return Json(ApiResponse.Fail("Only PDF documents (.pdf) are allowed for product attachment."));
                TempData["Error"] = "Only PDF documents (.pdf) are allowed.";
                return RedirectToAction(nameof(Product));
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents", "Products");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = Guid.NewGuid().ToString("N") + "_" + Path.GetFileName(pdfDocument.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await pdfDocument.CopyToAsync(fileStream);
            }
            input.DocumentPath = "/uploads/documents/Products/" + uniqueFileName;
        }

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Please correct the validation errors.", GetModelStateErrors()));
            TempData["Error"] = "Failed to save product. Check inputs.";
            return RedirectToAction(nameof(Product));
        }

        try
        {
            var product = new Product
            {
                Id = input.Id,
                ProductCode = input.ProductCode,
                ProductName = input.ProductName,
                CategoryId = input.CategoryId,
                BrandId = input.BrandId,
                UnitId = input.UnitId,
                WarehouseId = input.WarehouseId,
                HSNCode = input.HSNCode,
                Barcode = input.Barcode,
                PurchasePrice = input.PurchasePrice,
                SalesPrice = input.SalesPrice,
                MRP = input.MRP,
                GSTPercentage = input.GSTPercentage,
                MinimumStock = input.MinimumStock,
                ReorderLevel = input.ReorderLevel,
                Description = input.Description,
                DocumentPath = input.DocumentPath,
                IsActive = input.IsActive
            };
            var saved = await _masterService.SaveProductAsync(product);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Product saved successfully.", saved));
            TempData["Success"] = "Product saved successfully.";
            return RedirectToAction(nameof(Product));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Product));
        }
    }

    [HttpPost("DeleteProduct")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var (success, msg) = await _masterService.DeleteProductAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
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
            return Json(ApiResponse.Fail("Please select a valid Excel file."));
        }

        var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".xlsx" or ".xls"))
        {
            return Json(ApiResponse.Fail("Only Excel files (.xlsx, .xls) are supported."));
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

            return Json(ApiResponse.Ok($"Successfully uploaded {successCount} products."));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Fail("An error occurred: " + ex.Message));
        }
    }

    [HttpPost("ClearAllProductData")]
    public async Task<IActionResult> ClearAllProductData()
    {
        try
        {
            await _masterService.ClearAllProductDataAsync();
            return Json(ApiResponse.Ok("All product related data has been cleared successfully."));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Fail("An error occurred: " + ex.Message));
        }
    }

    [HttpPost("PreviewImport")]
    public async Task<IActionResult> PreviewImport(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Json(ApiResponse.Fail("Please select a valid Excel or PDF file."));
        }

        var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".xlsx" or ".xls" or ".pdf"))
        {
            return Json(ApiResponse.Fail("Only Excel (.xlsx, .xls) and PDF (.pdf) files are supported."));
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
            return Json(ApiResponse.Fail("An error occurred during parsing: " + ex.Message));
        }
    }

    [HttpPost("CommitImport")]
    public async Task<IActionResult> CommitImport([FromBody] List<ImportProductCommitDto> items)
    {
        if (items == null || !items.Any())
        {
            return Json(ApiResponse.Fail("No products provided for import."));
        }

        try
        {
            var list = await _masterService.CommitImportAsync(items);
            return Json(ApiResponse.Ok($"Successfully imported/updated {list.Count} products.", list));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Fail("An error occurred during import: " + ex.Message));
        }
    }

    // --- Category ---
    [HttpGet("Category")]
    public async Task<IActionResult> Category()
    {
        var list = await _masterService.GetCategoriesAsync();
        return View(new CategoryPageViewModel { Categories = list });
    }

    [HttpPost("SaveCategory")]
    public async Task<IActionResult> SaveCategory([FromForm] CategoryInputModel? model, [FromForm] CategoryInputModel? form, [FromForm] Category? legacyCategory)
    {
        var input = model?.CategoryName != null ? model : (form ?? model);
        var categoryName = input?.CategoryName ?? legacyCategory?.CategoryName ?? string.Empty;
        var id = input?.Id ?? legacyCategory?.Id ?? 0;
        var description = input?.Description ?? legacyCategory?.Description;

        if (string.IsNullOrWhiteSpace(categoryName))
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Category name is required."));
            TempData["Error"] = "Category name is required.";
            return RedirectToAction(nameof(Category));
        }

        try
        {
            var category = new Category
            {
                Id = id,
                CategoryName = categoryName,
                Description = description
            };
            var saved = await _masterService.SaveCategoryAsync(category);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Category saved successfully.", saved));
            TempData["Success"] = "Category saved successfully.";
            return RedirectToAction(nameof(Category));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Category));
        }
    }

    [HttpPost("DeleteCategory")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var (success, msg) = await _masterService.DeleteCategoryAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }

    // --- Brand ---
    [HttpGet("Brand")]
    public async Task<IActionResult> Brand()
    {
        var list = await _masterService.GetBrandsAsync();
        return View(new BrandPageViewModel { Brands = list });
    }

    [HttpPost("SaveBrand")]
    public async Task<IActionResult> SaveBrand([FromForm] BrandInputModel? model, [FromForm] BrandInputModel? form, [FromForm] Brand? legacyBrand)
    {
        var input = model?.BrandName != null ? model : (form ?? model);
        var brandName = input?.BrandName ?? legacyBrand?.BrandName ?? string.Empty;
        var id = input?.Id ?? legacyBrand?.Id ?? 0;
        var description = input?.Description ?? legacyBrand?.Description;

        if (string.IsNullOrWhiteSpace(brandName))
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Brand name is required."));
            TempData["Error"] = "Brand name is required.";
            return RedirectToAction(nameof(Brand));
        }

        try
        {
            var brand = new Brand
            {
                Id = id,
                BrandName = brandName,
                Description = description
            };
            var saved = await _masterService.SaveBrandAsync(brand);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Brand saved successfully.", saved));
            TempData["Success"] = "Brand saved successfully.";
            return RedirectToAction(nameof(Brand));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Brand));
        }
    }

    [HttpPost("DeleteBrand")]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        var (success, msg) = await _masterService.DeleteBrandAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }

    // --- Unit ---
    [HttpGet("Unit")]
    public async Task<IActionResult> Unit()
    {
        var list = await _masterService.GetUnitsAsync();
        return View(new UnitPageViewModel { Units = list });
    }

    [HttpPost("SaveUnit")]
    public async Task<IActionResult> SaveUnit([FromForm] UnitInputModel? model, [FromForm] UnitInputModel? form, [FromForm] Unit? legacyUnit)
    {
        var input = model?.UnitName != null ? model : (form ?? model);
        var unitName = input?.UnitName ?? legacyUnit?.UnitName ?? string.Empty;
        var unitSymbol = input?.UnitSymbol ?? legacyUnit?.UnitSymbol ?? string.Empty;
        var id = input?.Id ?? legacyUnit?.Id ?? 0;
        var description = input?.Description ?? legacyUnit?.Description;

        if (string.IsNullOrWhiteSpace(unitName))
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Unit name is required."));
            TempData["Error"] = "Unit name is required.";
            return RedirectToAction(nameof(Unit));
        }

        try
        {
            var unit = new Unit
            {
                Id = id,
                UnitName = unitName,
                UnitSymbol = unitSymbol,
                Description = description
            };
            var saved = await _masterService.SaveUnitAsync(unit);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Unit saved successfully.", saved));
            TempData["Success"] = "Unit saved successfully.";
            return RedirectToAction(nameof(Unit));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Unit));
        }
    }

    [HttpPost("DeleteUnit")]
    public async Task<IActionResult> DeleteUnit(int id)
    {
        var (success, msg) = await _masterService.DeleteUnitAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }

    // --- Warehouse ---
    [HttpGet("Warehouse")]
    public async Task<IActionResult> Warehouse()
    {
        var list = await _masterService.GetWarehousesAsync();
        return View(new WarehousePageViewModel { Warehouses = list });
    }

    [HttpPost("SaveWarehouse")]
    public async Task<IActionResult> SaveWarehouse([FromForm] WarehouseInputModel? model, [FromForm] WarehouseInputModel? form)
    {
        var input = model?.WarehouseCode != null ? model : (form ?? model ?? new WarehouseInputModel());
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Please correct the validation errors.", GetModelStateErrors()));
            return RedirectToAction(nameof(Warehouse));
        }

        try
        {
            var warehouse = new Warehouse
            {
                Id = input.Id,
                WarehouseCode = input.WarehouseCode,
                WarehouseName = input.WarehouseName,
                Address = input.Address,
                City = input.City,
                State = input.State,
                Phone = input.Phone,
                IsActive = input.IsActive
            };
            var saved = await _masterService.SaveWarehouseAsync(warehouse);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Warehouse saved successfully.", saved));
            TempData["Success"] = "Warehouse saved successfully.";
            return RedirectToAction(nameof(Warehouse));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Warehouse));
        }
    }

    [HttpPost("DeleteWarehouse")]
    public async Task<IActionResult> DeleteWarehouse(int id)
    {
        var (success, msg) = await _masterService.DeleteWarehouseAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }

    // --- Employee ---
    [HttpGet("Employee")]
    public async Task<IActionResult> Employee()
    {
        var list = await _masterService.GetEmployeesAsync();
        return View(new EmployeePageViewModel { Employees = list });
    }

    [HttpPost("SaveEmployee")]
    public async Task<IActionResult> SaveEmployee([FromForm] EmployeeInputModel? model, [FromForm] EmployeeInputModel? form, [FromForm] Employee? legacyEmployee)
    {
        var input = model?.EmployeeCode != null ? model : (form ?? model);
        var employeeCode = input?.EmployeeCode ?? legacyEmployee?.EmployeeCode ?? string.Empty;
        var employeeName = input?.EmployeeName ?? legacyEmployee?.EmployeeName ?? string.Empty;
        var id = input?.Id ?? legacyEmployee?.Id ?? 0;

        if (string.IsNullOrWhiteSpace(employeeCode) || string.IsNullOrWhiteSpace(employeeName))
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Employee code and name are required."));
            TempData["Error"] = "Employee code and name are required.";
            return RedirectToAction(nameof(Employee));
        }

        try
        {
            var employee = new Employee
            {
                Id = id,
                EmployeeCode = employeeCode,
                EmployeeName = employeeName,
                Designation = input?.Designation ?? legacyEmployee?.Designation,
                Department = input?.Department ?? legacyEmployee?.Department,
                Mobile = input?.Mobile ?? legacyEmployee?.Mobile,
                Email = input?.Email ?? legacyEmployee?.Email
            };
            var saved = await _masterService.SaveEmployeeAsync(employee);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Employee saved successfully.", saved));
            TempData["Success"] = "Employee saved successfully.";
            return RedirectToAction(nameof(Employee));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Employee));
        }
    }

    [HttpPost("DeleteEmployee")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var (success, msg) = await _masterService.DeleteEmployeeAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }

    // --- Account Group ---
    [HttpGet("AccountGroup")]
    public async Task<IActionResult> AccountGroup()
    {
        var list = await _masterService.GetAccountGroupsAsync();
        return View(new AccountGroupPageViewModel { AccountGroups = list });
    }

    [HttpPost("SaveAccountGroup")]
    public async Task<IActionResult> SaveAccountGroup([FromForm] AccountGroupInputModel? model, [FromForm] AccountGroupInputModel? form, [FromForm] AccountGroup? legacyGroup)
    {
        var input = model?.GroupName != null ? model : (form ?? model);
        var groupName = input?.GroupName ?? legacyGroup?.GroupName ?? string.Empty;
        var groupType = input?.GroupType ?? legacyGroup?.GroupType ?? "Asset";
        var id = input?.Id ?? legacyGroup?.Id ?? 0;
        var description = input?.Description ?? legacyGroup?.Description;

        if (string.IsNullOrWhiteSpace(groupName))
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Group name is required."));
            TempData["Error"] = "Group name is required.";
            return RedirectToAction(nameof(AccountGroup));
        }

        try
        {
            var group = new AccountGroup
            {
                Id = id,
                GroupName = groupName,
                GroupType = groupType,
                Description = description
            };
            var saved = await _masterService.SaveAccountGroupAsync(group);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Account Group saved successfully.", saved));
            TempData["Success"] = "Account Group saved successfully.";
            return RedirectToAction(nameof(AccountGroup));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(AccountGroup));
        }
    }

    [HttpPost("DeleteAccountGroup")]
    public async Task<IActionResult> DeleteAccountGroup(int id)
    {
        var (success, msg) = await _masterService.DeleteAccountGroupAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
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
    public async Task<IActionResult> SaveLedger([FromForm] LedgerInputModel? model, [FromForm] LedgerInputModel? form, [FromForm] Ledger? legacyLedger)
    {
        var input = model?.LedgerCode != null ? model : (form ?? model);
        var ledgerCode = input?.LedgerCode ?? legacyLedger?.LedgerCode ?? string.Empty;
        var ledgerName = input?.LedgerName ?? legacyLedger?.LedgerName ?? string.Empty;
        var accountGroupId = input?.AccountGroupId ?? legacyLedger?.AccountGroupId ?? 0;
        var id = input?.Id ?? legacyLedger?.Id ?? 0;

        if (string.IsNullOrWhiteSpace(ledgerCode) || string.IsNullOrWhiteSpace(ledgerName) || accountGroupId == 0)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Ledger code, name and account group are required."));
            TempData["Error"] = "Ledger code, name and account group are required.";
            return RedirectToAction(nameof(Ledger));
        }

        try
        {
            var ledger = new Ledger
            {
                Id = id,
                LedgerCode = ledgerCode,
                LedgerName = ledgerName,
                AccountGroupId = accountGroupId,
                OpeningBalance = input?.OpeningBalance ?? legacyLedger?.OpeningBalance ?? 0,
                BalanceType = input?.BalanceType ?? legacyLedger?.BalanceType ?? "Debit",
                Notes = input?.Description ?? legacyLedger?.Notes
            };
            var saved = await _masterService.SaveLedgerAsync(ledger);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Ledger saved successfully.", saved));
            TempData["Success"] = "Ledger saved successfully.";
            return RedirectToAction(nameof(Ledger));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Ledger));
        }
    }

    [HttpPost("DeleteLedger")]
    public async Task<IActionResult> DeleteLedger(int id)
    {
        var (success, msg) = await _masterService.DeleteLedgerAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
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
    public async Task<IActionResult> SaveBank([FromForm] BankInputModel? model, [FromForm] BankInputModel? form, [FromForm] Bank? legacyBank)
    {
        var input = model?.BankName != null ? model : (form ?? model);
        var bankName = input?.BankName ?? legacyBank?.BankName ?? string.Empty;
        var id = input?.Id ?? legacyBank?.Id ?? 0;

        if (string.IsNullOrWhiteSpace(bankName))
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Bank name is required."));
            TempData["Error"] = "Bank name is required.";
            return RedirectToAction(nameof(Bank));
        }

        try
        {
            var bank = new Bank
            {
                Id = id,
                BankName = bankName,
                BranchName = input?.BranchName ?? legacyBank?.BranchName,
                AccountNumber = input?.AccountNumber ?? legacyBank?.AccountNumber,
                IFSCCode = input?.IFSCCode ?? legacyBank?.IFSCCode,
                LedgerId = input?.LedgerId ?? legacyBank?.LedgerId
            };
            var saved = await _masterService.SaveBankAsync(bank);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Bank saved successfully.", saved));
            TempData["Success"] = "Bank saved successfully.";
            return RedirectToAction(nameof(Bank));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Bank));
        }
    }

    [HttpPost("DeleteBank")]
    public async Task<IActionResult> DeleteBank(int id)
    {
        var (success, msg) = await _masterService.DeleteBankAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }

    // --- Tax ---
    [HttpGet("Tax")]
    public async Task<IActionResult> Tax()
    {
        var list = await _masterService.GetTaxesAsync();
        return View(new TaxPageViewModel { Taxes = list });
    }

    [HttpPost("SaveTax")]
    public async Task<IActionResult> SaveTax([FromForm] TaxInputModel? model, [FromForm] TaxInputModel? form, [FromForm] Tax? legacyTax)
    {
        var input = model?.TaxName != null ? model : (form ?? model);
        var taxName = input?.TaxName ?? legacyTax?.TaxName ?? string.Empty;
        var id = input?.Id ?? legacyTax?.Id ?? 0;

        if (string.IsNullOrWhiteSpace(taxName))
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Tax name is required."));
            TempData["Error"] = "Tax name is required.";
            return RedirectToAction(nameof(Tax));
        }

        try
        {
            var tax = new Tax
            {
                Id = id,
                TaxName = taxName,
                TaxPercentage = input?.TaxPercentage ?? legacyTax?.TaxPercentage ?? 0,
                IGSTPercentage = input?.IGSTPercentage ?? legacyTax?.IGSTPercentage,
                CGSTPercentage = input?.CGSTPercentage ?? legacyTax?.CGSTPercentage,
                SGSTPercentage = input?.SGSTPercentage ?? legacyTax?.SGSTPercentage
            };
            var saved = await _masterService.SaveTaxAsync(tax);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Tax settings saved successfully.", saved));
            TempData["Success"] = "Tax settings saved successfully.";
            return RedirectToAction(nameof(Tax));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Tax));
        }
    }

    [HttpPost("DeleteTax")]
    public async Task<IActionResult> DeleteTax(int id)
    {
        var (success, msg) = await _masterService.DeleteTaxAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }

    // --- Payment Mode ---
    [HttpGet("PaymentMode")]
    public async Task<IActionResult> PaymentMode()
    {
        var list = await _masterService.GetPaymentModesAsync();
        return View(new PaymentModePageViewModel { PaymentModes = list });
    }

    [HttpPost("SavePaymentMode")]
    public async Task<IActionResult> SavePaymentMode([FromForm] PaymentModeInputModel? model, [FromForm] PaymentModeInputModel? form, [FromForm] PaymentMode? legacyMode)
    {
        var input = model?.ModeName != null ? model : (form ?? model);
        var modeName = input?.ModeName ?? legacyMode?.ModeName ?? string.Empty;
        var modeType = input?.ModeType ?? legacyMode?.ModeType ?? "Cash";
        var id = input?.Id ?? legacyMode?.Id ?? 0;

        if (string.IsNullOrWhiteSpace(modeName))
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Payment mode name is required."));
            TempData["Error"] = "Payment mode name is required.";
            return RedirectToAction(nameof(PaymentMode));
        }

        try
        {
            var mode = new PaymentMode
            {
                Id = id,
                ModeName = modeName,
                ModeType = modeType
            };
            var saved = await _masterService.SavePaymentModeAsync(mode);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Payment Mode saved successfully.", saved));
            TempData["Success"] = "Payment Mode saved successfully.";
            return RedirectToAction(nameof(PaymentMode));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(PaymentMode));
        }
    }

    [HttpPost("DeletePaymentMode")]
    public async Task<IActionResult> DeletePaymentMode(int id)
    {
        var (success, msg) = await _masterService.DeletePaymentModeAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }
}

