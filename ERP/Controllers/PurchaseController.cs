using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Filters;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize]
[Permission("Purchase", "View")]
[Route("Purchase")]
public class PurchaseController : Controller
{
    private readonly IPurchaseService _purchaseService;
    private readonly IMasterService _masterService;

    public PurchaseController(IPurchaseService purchaseService, IMasterService masterService)
    {
        _purchaseService = purchaseService;
        _masterService = masterService;
    }

    private async Task<PurchasePageViewModel<TItem>> BuildPageModel<TItem>(List<TItem> items)
    {
        var products = await _masterService.GetProductsAsync();
        return new PurchasePageViewModel<TItem>
        {
            Items = items,
            Suppliers = await _masterService.GetSuppliersAsync(),
            ProductLookups = products.Select(p => new ProductLookupViewModel
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                SalesPrice = p.SalesPrice,
                PurchasePrice = p.PurchasePrice,
                GSTPercentage = p.GSTPercentage,
                CurrentStock = p.CurrentStock,
                CategoryId = p.CategoryId
            }).ToList(),
            Warehouses = await _masterService.GetWarehousesAsync(),
            Categories = await _masterService.GetCategoriesAsync()
        };
    }

    // --- Purchase Order ---
    [HttpGet("Order")]
    public async Task<IActionResult> Order()
    {
        var list = await _purchaseService.GetPurchaseOrdersAsync();
        return View(await BuildPageModel(list));
    }

    [HttpPost("SaveOrder")]
    public async Task<IActionResult> SaveOrder([FromBody] PurchaseOrder order)
    {
        if (ModelState.IsValid)
        {
            await _purchaseService.SavePurchaseOrderAsync(order);
            return Json(new { success = true, message = "Purchase Order saved successfully." });
        }
        return Json(new { success = false, message = "Invalid data." });
    }

    [HttpPost("DeleteOrder")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        await _purchaseService.DeletePurchaseOrderAsync(id);
        return Json(new { success = true });
    }

    [HttpPost("UploadDocument")]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Json(new { success = false, message = "File not selected" });

        try
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents", "PurchaseOrders");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return Json(new { success = true, filePath = "/uploads/documents/PurchaseOrders/" + uniqueFileName });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("ConvertOrderToInvoice")]
    public async Task<IActionResult> ConvertOrderToInvoice(int id)
    {
        try
        {
            var order = await _purchaseService.GetPurchaseOrderByIdAsync(id);
            if (order == null || !order.IsActive)
            {
                return Json(new { success = false, message = "Purchase order not found." });
            }

            var invoice = new PurchaseInvoice
            {
                InvoiceNumber = await _masterService.ReserveNextBillNumberAsync("purchase"),
                InvoiceDate = DateTime.Today,
                SupplierId = order.SupplierId,
                PurchaseOrderId = order.Id,
                ReferenceNumber = order.OrderNumber,
                Status = "Issued",
                Notes = order.Notes,
                SubTotal = order.SubTotal,
                TaxAmount = order.TaxAmount,
                DiscountAmount = order.DiscountAmount,
                GrandTotal = order.GrandTotal,
                BalanceAmount = order.GrandTotal
            };

            foreach (var item in order.Items)
            {
                invoice.Items.Add(new PurchaseInvoiceItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitName = item.UnitName,
                    Quantity = item.Quantity,
                    Rate = item.Rate,
                    Discount = item.Discount,
                    TaxPercentage = item.TaxPercentage,
                    TaxAmount = item.TaxAmount,
                    Amount = item.Amount == 0 ? item.Quantity * item.Rate + item.TaxAmount : item.Amount,
                    SortOrder = item.SortOrder
                });
            }

            await _purchaseService.SaveInvoiceAsync(invoice);
            await _purchaseService.UpdatePurchaseOrderStatusAsync(order.Id, "Completed");

            return Json(new { success = true, message = $"Purchase order converted to invoice {invoice.InvoiceNumber}." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // --- Goods Receipt Note (GRN) ---
    [HttpGet("GRN")]
    public async Task<IActionResult> GRN()
    {
        var list = await _purchaseService.GetGRNsAsync();
        return View(await BuildPageModel(list));
    }

    [HttpPost("SaveGRN")]
    public async Task<IActionResult> SaveGRN([FromBody] GoodsReceiptNote grn)
    {
        if (ModelState.IsValid)
        {
            await _purchaseService.SaveGRNAsync(grn);
            return Json(new { success = true, message = "Goods Receipt Note saved successfully." });
        }
        return Json(new { success = false, message = "Invalid data." });
    }

    [HttpPost("DeleteGRN")]
    public async Task<IActionResult> DeleteGRN(int id)
    {
        await _purchaseService.DeleteGRNAsync(id);
        return Json(new { success = true });
    }

    // --- Purchase Invoice ---
    [HttpGet("Invoice")]
    public async Task<IActionResult> Invoice()
    {
        var list = await _purchaseService.GetInvoicesAsync();
        return View(await BuildPageModel(list));
    }

    [HttpPost("SaveInvoice")]
    public async Task<IActionResult> SaveInvoice([FromBody] PurchaseInvoice invoice)
    {
        invoice.Supplier = null;
        if (invoice.Items != null)
        {
            foreach (var item in invoice.Items)
            {
                item.Product = null;
                item.PurchaseInvoice = null;
            }
        }

        foreach (var key in ModelState.Keys.Where(k => k.Contains("ProductName", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            ModelState.Remove(key);
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (invoice.Id == 0)
                {
                    invoice.InvoiceNumber = await _masterService.ReserveNextBillNumberAsync("purchase");
                }

                await _purchaseService.SaveInvoiceAsync(invoice);
                return Json(new { success = true, message = "Purchase Invoice saved successfully.", invoiceNumber = invoice.InvoiceNumber });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message });
            }
        }

        var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        return Json(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Invalid data." : errors });
    }

    [HttpGet("GetInvoice/{id}")]
    public async Task<IActionResult> GetInvoice(int id)
    {
        var invoice = await _purchaseService.GetInvoiceByIdAsync(id);
        if (invoice == null) return NotFound();
        return Json(invoice);
    }

    [HttpPost("DeleteInvoice")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        await _purchaseService.DeleteInvoiceAsync(id);
        return Json(new { success = true });
    }

    // --- Purchase Return ---
    [HttpGet("Return")]
    public async Task<IActionResult> Return()
    {
        var list = await _purchaseService.GetReturnsAsync();
        return View(await BuildPageModel(list));
    }

    [HttpPost("SaveReturn")]
    public async Task<IActionResult> SaveReturn([FromBody] PurchaseReturn purchaseReturn)
    {
        if (ModelState.IsValid)
        {
            await _purchaseService.SaveReturnAsync(purchaseReturn);
            return Json(new { success = true, message = "Purchase Return saved successfully." });
        }
        return Json(new { success = false, message = "Invalid data." });
    }

    [HttpPost("DeleteReturn")]
    public async Task<IActionResult> DeleteReturn(int id)
    {
        await _purchaseService.DeleteReturnAsync(id);
        return Json(new { success = true });
    }

    [HttpGet("GetOrder/{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _purchaseService.GetPurchaseOrderByIdAsync(id);
        if (order == null) return NotFound();
        return Json(order);
    }

    [HttpGet("GetGRN/{id}")]
    public async Task<IActionResult> GetGRN(int id)
    {
        var item = await _purchaseService.GetGRNByIdAsync(id);
        if (item == null) return NotFound();
        return Json(item);
    }

    [HttpGet("GetReturn/{id}")]
    public async Task<IActionResult> GetReturn(int id)
    {
        var item = await _purchaseService.GetReturnByIdAsync(id);
        if (item == null) return NotFound();
        return Json(item);
    }

    [HttpPost("ProcessUploadedProducts")]
    public async Task<IActionResult> ProcessUploadedProducts([FromBody] List<UploadedProductItemDto> items)
    {
        if (items == null || !items.Any())
        {
            return Json(new { success = false, message = "No items parsed from image." });
        }

        try
        {
            var resultList = new List<object>();
            
            // Get all active categories, brands, units, warehouses for lookups
            var categories = await _masterService.GetCategoriesAsync();
            var brands = await _masterService.GetBrandsAsync();
            var units = await _masterService.GetUnitsAsync();
            var warehouses = await _masterService.GetWarehousesAsync();
            var products = await _masterService.GetProductsAsync();

            // Ensure we have a default warehouse, brand, unit
            var defaultWarehouse = warehouses.FirstOrDefault() ?? new Warehouse { WarehouseCode = "MAIN-WH", WarehouseName = "Main Central Warehouse" };
            if (defaultWarehouse.Id == 0)
            {
                defaultWarehouse = await _masterService.SaveWarehouseAsync(defaultWarehouse);
            }

            var defaultBrand = brands.FirstOrDefault(b => b.BrandName == "Generic") ?? new Brand { BrandName = "Generic" };
            if (defaultBrand.Id == 0)
            {
                defaultBrand = await _masterService.SaveBrandAsync(defaultBrand);
            }

            var defaultUnit = units.FirstOrDefault(u => u.UnitSymbol == "PCS") ?? new Unit { UnitName = "Pieces", UnitSymbol = "PCS" };
            if (defaultUnit.Id == 0)
            {
                defaultUnit = await _masterService.SaveUnitAsync(defaultUnit);
            }

            foreach (var item in items)
            {
                var name = item.ProductName.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                // Check if product exists (case-insensitive)
                var product = products.FirstOrDefault(p => p.ProductName.Equals(name, StringComparison.OrdinalIgnoreCase) && p.IsActive);

                if (product == null)
                {
                    // Determine Category name based on description
                    var categoryName = "General";
                    var lowerName = name.ToLowerInvariant();
                    if (lowerName.Contains("spanner") || lowerName.Contains("ring") || lowerName.Contains("sp r/s") || lowerName.Contains("wrench") || lowerName.Contains("sp "))
                    {
                        categoryName = "Spanners & Wrenches";
                    }
                    else if (lowerName.Contains("drill") || lowerName.Contains("hammer bit") || lowerName.Contains("bit") || lowerName.Contains("sds"))
                    {
                        categoryName = "Drill & Hammer Bits";
                    }
                    else if (lowerName.Contains("power tool") || lowerName.Contains("drill machine") || lowerName.Contains("saw"))
                    {
                        categoryName = "Power Tools";
                    }
                    else if (lowerName.Contains("hammer") || lowerName.Contains("pliers") || lowerName.Contains("screwdriver"))
                    {
                        categoryName = "Hand Tools";
                    }

                    var category = categories.FirstOrDefault(c => c.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                    if (category == null)
                    {
                        category = new Category { CategoryName = categoryName };
                        category = await _masterService.SaveCategoryAsync(category);
                        categories = await _masterService.GetCategoriesAsync(); // Refresh list
                    }

                    // Determine Brand from name (e.g. Venus, ALKO PLUS, Ultra Touch)
                    var brandName = "Generic";
                    if (lowerName.Contains("venus")) brandName = "Venus";
                    else if (lowerName.Contains("alko plus")) brandName = "ALKO PLUS";
                    else if (lowerName.Contains("ultra touch")) brandName = "Ultra Touch";
                    else if (lowerName.Contains("saw master")) brandName = "Saw Master";
                    else if (lowerName.Contains("hi-smart")) brandName = "Hi-Smart";

                    var brand = brands.FirstOrDefault(b => b.BrandName.Equals(brandName, StringComparison.OrdinalIgnoreCase));
                    if (brand == null)
                    {
                        brand = new Brand { BrandName = brandName };
                        brand = await _masterService.SaveBrandAsync(brand);
                        brands = await _masterService.GetBrandsAsync(); // Refresh list
                    }

                    // Create the new product
                    var nextId = products.Count + 1;
                    product = new Product
                    {
                        ProductCode = $"PRD{DateTime.Now:yyyy}{nextId:0000}",
                        ProductName = name,
                        CategoryId = category.Id,
                        BrandId = brand.Id,
                        UnitId = defaultUnit.Id,
                        WarehouseId = defaultWarehouse.Id,
                        PurchasePrice = item.Rate,
                        SalesPrice = Math.Round(item.Rate * 1.25m, 2), // 25% markup
                        MRP = Math.Round(item.Rate * 1.30m, 2),        // 30% markup
                        GSTPercentage = 18,                            // Default GST
                        OpeningStock = 0,
                        CurrentStock = 0,
                        IsActive = true
                    };

                    product = await _masterService.SaveProductAsync(product);
                    products = await _masterService.GetProductsAsync(); // Refresh list
                }

                resultList.Add(new
                {
                    productId = product.Id,
                    productName = product.ProductName,
                    categoryId = product.CategoryId,
                    rate = product.PurchasePrice,
                    qty = item.Qty
                });
            }

            return Json(new { success = true, items = resultList });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}

public class UploadedProductItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
}
