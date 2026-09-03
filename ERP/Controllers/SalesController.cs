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

    private Dictionary<string, string> GetModelStateErrors() =>
        ModelState.Where(x => x.Value?.Errors.Count > 0)
                  .ToDictionary(
                      k => k.Key,
                      v => v.Value!.Errors.First().ErrorMessage
                  );

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
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse.Fail("Please correct the highlighted validation errors.", GetModelStateErrors()));
        }

        try
        {
            var saved = await _salesService.SaveQuotationAsync(quotation);
            return Json(ApiResponse.Ok("Sales Quotation saved successfully.", saved));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("DeleteQuotation")]
    public async Task<IActionResult> DeleteQuotation(int id)
    {
        var (success, msg) = await _salesService.DeleteQuotationAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
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
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse.Fail("Please correct the highlighted validation errors.", GetModelStateErrors()));
        }

        try
        {
            var saved = await _salesService.SaveSalesOrderAsync(order);
            return Json(ApiResponse.Ok("Sales Order saved successfully.", saved));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("DeleteOrder")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var (success, msg) = await _salesService.DeleteSalesOrderAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }

    [HttpPost("ConvertOrderToInvoice")]
    public async Task<IActionResult> ConvertOrderToInvoice(int id)
    {
        try
        {
            var order = await _salesService.GetSalesOrderByIdAsync(id);
            if (order == null || !order.IsActive)
            {
                return Json(ApiResponse.Fail("Sales order not found."));
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

            return Json(ApiResponse.Ok($"Sales order converted to invoice {invoice.InvoiceNumber}.", invoice));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Fail(ex.Message));
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
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse.Fail("Please correct the highlighted validation errors.", GetModelStateErrors()));
        }

        try
        {
            var saved = await _salesService.SaveDeliveryChallanAsync(challan);
            return Json(ApiResponse.Ok("Delivery Challan saved successfully.", saved));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("DeleteChallan")]
    public async Task<IActionResult> DeleteChallan(int id)
    {
        var (success, msg) = await _salesService.DeleteDeliveryChallanAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
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

        if (!ModelState.IsValid)
        {
            return Json(ApiResponse.Fail("Please correct the validation errors.", GetModelStateErrors()));
        }

        try
        {
            if (invoice.Id == 0)
            {
                invoice.InvoiceNumber = await _masterService.ReserveNextBillNumberAsync("sales");
            }

            await _salesService.SaveInvoiceAsync(invoice);
            return Json(ApiResponse.Ok("Sales Invoice saved successfully.", new { invoiceNumber = invoice.InvoiceNumber, id = invoice.Id }));
        }
        catch (Exception ex)
        {
            var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return Json(ApiResponse.Fail(message));
        }
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
        var (success, msg) = await _salesService.DeleteInvoiceAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
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
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse.Fail("Please correct the highlighted validation errors.", GetModelStateErrors()));
        }

        try
        {
            var saved = await _salesService.SaveReturnAsync(salesReturn);
            return Json(ApiResponse.Ok("Sales Return saved successfully.", saved));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("DeleteReturn")]
    public async Task<IActionResult> DeleteReturn(int id)
    {
        var (success, msg) = await _salesService.DeleteReturnAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }
}
