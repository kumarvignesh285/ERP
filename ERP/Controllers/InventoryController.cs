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

    private Dictionary<string, string> GetModelStateErrors() =>
        ModelState.Where(x => x.Value?.Errors.Count > 0)
                  .ToDictionary(
                      k => k.Key,
                      v => v.Value!.Errors.First().ErrorMessage
                  );

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
        try
        {
            await _inventoryService.UpdateOpeningStockAsync(productId, quantity);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(ApiResponse.Ok("Opening Stock updated successfully."));
            TempData["Success"] = "Opening Stock updated successfully.";
            return RedirectToAction(nameof(StockOpening));
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(StockOpening));
        }
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
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse.Fail("Please correct the highlighted validation errors.", GetModelStateErrors()));
        }

        try
        {
            var saved = await _inventoryService.SaveStockTransferAsync(transfer);
            return Json(ApiResponse.Ok("Stock Transfer saved successfully.", saved));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("DeleteStockTransfer")]
    public async Task<IActionResult> DeleteStockTransfer(int id)
    {
        var (success, msg) = await _inventoryService.DeleteStockTransferAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
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
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse.Fail("Please correct the highlighted validation errors.", GetModelStateErrors()));
        }

        try
        {
            var saved = await _inventoryService.SaveStockAdjustmentAsync(adjustment);
            return Json(ApiResponse.Ok("Stock Adjustment saved successfully.", saved));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("DeleteStockAdjustment")]
    public async Task<IActionResult> DeleteStockAdjustment(int id)
    {
        var (success, msg) = await _inventoryService.DeleteStockAdjustmentAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
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
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse.Fail("Please correct the highlighted validation errors.", GetModelStateErrors()));
        }

        try
        {
            var saved = await _inventoryService.SavePhysicalStockVerificationAsync(verification);
            return Json(ApiResponse.Ok("Physical stock verification saved successfully.", saved));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("DeletePhysicalVerification")]
    public async Task<IActionResult> DeletePhysicalVerification(int id)
    {
        var (success, msg) = await _inventoryService.DeletePhysicalStockVerificationAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }
}
