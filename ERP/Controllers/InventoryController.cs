using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Filters;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize]
[Permission("Inventory", "View")]
[Route("Inventory")]
public class InventoryController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IMasterService _masterService;

    public InventoryController(IInventoryService inventoryService, IMasterService masterService)
    {
        _inventoryService = inventoryService;
        _masterService = masterService;
    }

    private async Task<InventoryPageViewModel<TItem>> BuildPageModel<TItem>(List<TItem> items)
    {
        var products = await _masterService.GetProductsAsync();
        return new InventoryPageViewModel<TItem>
        {
            Items = items,
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

    // --- Stock Opening ---
    [HttpGet("StockOpening")]
    public async Task<IActionResult> StockOpening()
    {
        var list = await _inventoryService.GetProductsForOpeningStockAsync();
        return View(list);
    }

    [HttpPost("SaveStockOpening")]
    public async Task<IActionResult> SaveStockOpening(int productId, decimal quantity)
    {
        await _inventoryService.UpdateOpeningStockAsync(productId, quantity);
        TempData["Success"] = "Opening Stock updated successfully.";
        return RedirectToAction(nameof(StockOpening));
    }

    // --- Stock Transfer ---
    [HttpGet("StockTransfer")]
    public async Task<IActionResult> StockTransfer()
    {
        var list = await _inventoryService.GetStockTransfersAsync();
        return View(await BuildPageModel(list));
    }

    [HttpPost("SaveStockTransfer")]
    public async Task<IActionResult> SaveStockTransfer([FromBody] StockTransfer transfer)
    {
        if (ModelState.IsValid)
        {
            await _inventoryService.SaveStockTransferAsync(transfer);
            return Json(new { success = true, message = "Stock Transfer saved successfully." });
        }
        return Json(new { success = false, message = "Invalid data." });
    }

    [HttpPost("DeleteStockTransfer")]
    public async Task<IActionResult> DeleteStockTransfer(int id)
    {
        await _inventoryService.DeleteStockTransferAsync(id);
        return Json(new { success = true });
    }

    // --- Stock Adjustment ---
    [HttpGet("StockAdjustment")]
    public async Task<IActionResult> StockAdjustment()
    {
        var list = await _inventoryService.GetStockAdjustmentsAsync();
        return View(await BuildPageModel(list));
    }

    [HttpPost("SaveStockAdjustment")]
    public async Task<IActionResult> SaveStockAdjustment([FromBody] StockAdjustment adjustment)
    {
        if (ModelState.IsValid)
        {
            await _inventoryService.SaveStockAdjustmentAsync(adjustment);
            return Json(new { success = true, message = "Stock Adjustment saved successfully." });
        }
        return Json(new { success = false, message = "Invalid data." });
    }

    [HttpPost("DeleteStockAdjustment")]
    public async Task<IActionResult> DeleteStockAdjustment(int id)
    {
        await _inventoryService.DeleteStockAdjustmentAsync(id);
        return Json(new { success = true });
    }

    // --- Physical Stock Verification ---
    [HttpGet("PhysicalVerification")]
    public async Task<IActionResult> PhysicalVerification()
    {
        var list = await _inventoryService.GetPhysicalStockVerificationsAsync();
        return View(await BuildPageModel(list));
    }

    [HttpPost("SavePhysicalVerification")]
    public async Task<IActionResult> SavePhysicalVerification([FromBody] PhysicalStockVerification verification)
    {
        if (ModelState.IsValid)
        {
            await _inventoryService.SavePhysicalStockVerificationAsync(verification);
            return Json(new { success = true, message = "Physical stock verification saved successfully." });
        }
        return Json(new { success = false, message = "Invalid data." });
    }

    [HttpPost("DeletePhysicalVerification")]
    public async Task<IActionResult> DeletePhysicalVerification(int id)
    {
        await _inventoryService.DeletePhysicalStockVerificationAsync(id);
        return Json(new { success = true });
    }
}
