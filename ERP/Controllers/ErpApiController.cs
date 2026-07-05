using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;

namespace ERP.Controllers;

[ApiController]
[Route("api/erp")]
public class ErpApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMasterService _masterService;

    public ErpApiController(AppDbContext context, IMasterService masterService)
    {
        _context = context;
        _masterService = masterService;
    }

    [HttpGet("company")]
    public async Task<IActionResult> GetCompany()
    {
        var company = await _masterService.GetCompanyAsync();
        if (company == null) return Ok(new { });
        return Ok(new
        {
            company.Id,
            company.CompanyName,
            company.CompanyCode,
            company.Address,
            company.City,
            company.State,
            company.Country,
            company.Phone,
            company.Email,
            company.Website,
            company.GSTNumber,
            company.PANNumber,
            company.Logo,
            company.BillType,
            company.BillFooterNote,
            company.BankDetails,
            company.SalesBillPrefix,
            company.SalesBillNextNumber,
            company.PurchaseBillPrefix,
            company.PurchaseBillNextNumber
        });
    }

    [HttpGet("next-bill-number")]
    public async Task<IActionResult> GetNextBillNumber([FromQuery] string type = "sales")
    {
        var number = await _masterService.GetNextBillNumberPreviewAsync(type);
        return Ok(new { billNumber = number });
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        var list = await _context.Notifications
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var list = await _masterService.GetProductsAsync();
        return Ok(list);
    }

    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpGet("customers/{id}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();
        return Ok(customer);
    }

    [HttpGet("suppliers/{id}")]
    public async Task<IActionResult> GetSupplier(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();
        return Ok(supplier);
    }
}
