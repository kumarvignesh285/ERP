using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize]
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
                return Json(new { success = false, message = ex.Message });
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
}
