using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Filters;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize]
[Permission("Sales", "View")]
[Route("Sales")]
public class SalesController : Controller
{
    private readonly ISalesService _salesService;
    private readonly IMasterService _masterService;

    public SalesController(ISalesService salesService, IMasterService masterService)
    {
        _salesService = salesService;
        _masterService = masterService;
    }

    private async Task<SalesPageViewModel<TItem>> BuildPageModel<TItem>(List<TItem> items)
    {
        var products = await _masterService.GetProductsAsync();
        return new SalesPageViewModel<TItem>
        {
            Items = items,
            Customers = await _masterService.GetCustomersAsync(),
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

    // --- Sales Quotation ---
    [HttpGet("Quotation")]
    public async Task<IActionResult> Quotation()
    {
        var list = await _salesService.GetQuotationsAsync();
        return View(await BuildPageModel(list));
    }

    [HttpPost("SaveQuotation")]
    public async Task<IActionResult> SaveQuotation([FromBody] SalesQuotation quotation)
    {
        if (ModelState.IsValid)
        {
            await _salesService.SaveQuotationAsync(quotation);
            return Json(new { success = true, message = "Sales Quotation saved successfully." });
        }
        return Json(new { success = false, message = "Invalid data model." });
    }

    [HttpPost("DeleteQuotation")]
    public async Task<IActionResult> DeleteQuotation(int id)
    {
        await _salesService.DeleteQuotationAsync(id);
        return Json(new { success = true });
    }

    // --- Sales Order ---
    [HttpGet("Order")]
    public async Task<IActionResult> Order()
    {
        var list = await _salesService.GetSalesOrdersAsync();
        return View(await BuildPageModel(list));
    }

    [HttpPost("SaveOrder")]
    public async Task<IActionResult> SaveOrder([FromBody] SalesOrder order)
    {
        if (ModelState.IsValid)
        {
            await _salesService.SaveSalesOrderAsync(order);
            return Json(new { success = true, message = "Sales Order saved successfully." });
        }
        return Json(new { success = false, message = "Invalid data." });
    }

    [HttpPost("DeleteOrder")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        await _salesService.DeleteSalesOrderAsync(id);
        return Json(new { success = true });
    }

    [HttpPost("ConvertOrderToInvoice")]
    public async Task<IActionResult> ConvertOrderToInvoice(int id)
    {
        try
        {
            var order = await _salesService.GetSalesOrderByIdAsync(id);
            if (order == null || !order.IsActive)
            {
                return Json(new { success = false, message = "Sales order not found." });
            }

            var invoice = new SalesInvoice
            {
                InvoiceNumber = await _masterService.ReserveNextBillNumberAsync("sales"),
                InvoiceDate = DateTime.Today,
                CustomerId = order.CustomerId,
                SalesOrderId = order.Id,
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
                invoice.Items.Add(new SalesInvoiceItem
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

            await _salesService.SaveInvoiceAsync(invoice);
            await _salesService.UpdateSalesOrderStatusAsync(order.Id, "Completed");

            return Json(new { success = true, message = $"Sales order converted to invoice {invoice.InvoiceNumber}." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // --- Delivery Challan ---
    [HttpGet("Challan")]
    public async Task<IActionResult> Challan()
    {
        var list = await _salesService.GetDeliveryChallansAsync();
        return View(await BuildPageModel(list));
    }

    [HttpPost("SaveChallan")]
    public async Task<IActionResult> SaveChallan([FromBody] DeliveryChallan challan)
    {
        if (ModelState.IsValid)
        {
            await _salesService.SaveDeliveryChallanAsync(challan);
            return Json(new { success = true, message = "Delivery Challan saved successfully." });
        }
        return Json(new { success = false, message = "Invalid data." });
    }

    [HttpPost("DeleteChallan")]
    public async Task<IActionResult> DeleteChallan(int id)
    {
        await _salesService.DeleteDeliveryChallanAsync(id);
        return Json(new { success = true });
    }

    // --- Sales Invoice ---
    [HttpGet("Invoice")]
    public async Task<IActionResult> Invoice()
    {
        var list = await _salesService.GetInvoicesAsync();
        return View(await BuildPageModel(list));
    }

    [HttpPost("SaveInvoice")]
    public async Task<IActionResult> SaveInvoice([FromBody] SalesInvoice invoice)
    {
        invoice.Customer = null;
        if (invoice.Items != null)
        {
            foreach (var item in invoice.Items)
            {
                item.Product = null;
                item.SalesInvoice = null;
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
                    invoice.InvoiceNumber = await _masterService.ReserveNextBillNumberAsync("sales");
                }

                await _salesService.SaveInvoiceAsync(invoice);
                return Json(new { success = true, message = "Sales Invoice saved successfully.", invoiceNumber = invoice.InvoiceNumber });
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
        var invoice = await _salesService.GetInvoiceByIdAsync(id);
        if (invoice == null) return NotFound();
        return Json(invoice);
    }

    [HttpPost("DeleteInvoice")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        await _salesService.DeleteInvoiceAsync(id);
        return Json(new { success = true });
    }

    [HttpGet("GetOrder/{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _salesService.GetSalesOrderByIdAsync(id);
        if (order == null) return NotFound();
        return Json(order);
    }

    [HttpGet("GetQuotation/{id}")]
    public async Task<IActionResult> GetQuotation(int id)
    {
        var item = await _salesService.GetQuotationByIdAsync(id);
        if (item == null) return NotFound();
        return Json(item);
    }

    [HttpGet("GetChallan/{id}")]
    public async Task<IActionResult> GetChallan(int id)
    {
        var item = await _salesService.GetDeliveryChallanByIdAsync(id);
        if (item == null) return NotFound();
        return Json(item);
    }

    [HttpGet("GetReturn/{id}")]
    public async Task<IActionResult> GetReturn(int id)
    {
        var item = await _salesService.GetReturnByIdAsync(id);
        if (item == null) return NotFound();
        return Json(item);
    }

    // --- Sales Return ---
    [HttpGet("Return")]
    public async Task<IActionResult> Return()
    {
        var list = await _salesService.GetReturnsAsync();
        return View(await BuildPageModel(list));
    }

    [HttpPost("SaveReturn")]
    public async Task<IActionResult> SaveReturn([FromBody] SalesReturn salesReturn)
    {
        if (ModelState.IsValid)
        {
            await _salesService.SaveReturnAsync(salesReturn);
            return Json(new { success = true, message = "Sales Return saved successfully." });
        }
        return Json(new { success = false, message = "Invalid data." });
    }

    [HttpPost("DeleteReturn")]
    public async Task<IActionResult> DeleteReturn(int id)
    {
        await _salesService.DeleteReturnAsync(id);
        return Json(new { success = true });
    }
}
