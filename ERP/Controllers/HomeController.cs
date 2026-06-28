using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    [Route("")]
    [Route("Home/Index")]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        
        var dbPendingApprovals = await _context.SalesOrders.Where(o => o.IsActive && o.Status == "Pending").CountAsync();
        var dbAlertsCount = await _context.Notifications.CountAsync(n => !n.IsRead);
        var dbAlertsList = await _context.Notifications.Where(n => !n.IsRead).OrderByDescending(n => n.CreatedAt).Take(4).ToListAsync();

        return View(new HomeDashboardViewModel
        {
            TodaySales = await _context.SalesInvoices
                .Where(i => i.IsActive && i.InvoiceDate >= today)
                .SumAsync(i => i.GrandTotal),
            TodayPurchases = await _context.PurchaseInvoices
                .Where(i => i.IsActive && i.InvoiceDate >= today)
                .SumAsync(i => i.GrandTotal),
            TotalReceivables = await _context.SalesInvoices
                .Where(i => i.IsActive)
                .SumAsync(i => i.BalanceAmount),
            TotalPayables = await _context.PurchaseInvoices
                .Where(i => i.IsActive)
                .SumAsync(i => i.BalanceAmount),
            AvailableStockValue = await _context.Products
                .Where(p => p.IsActive)
                .SumAsync(p => p.CurrentStock * p.PurchasePrice),
            PendingApprovalsCount = dbPendingApprovals,
            CustomerCount = await _context.Customers.CountAsync(c => c.IsActive),
            SupplierCount = await _context.Suppliers.CountAsync(s => s.IsActive),
            ProductCount = await _context.Products.CountAsync(p => p.IsActive),
            OpenSalesOrders = await _context.SalesOrders.CountAsync(o => o.IsActive && o.Status != "Completed" && o.Status != "Cancelled"),
            OpenPurchaseOrders = await _context.PurchaseOrders.CountAsync(o => o.IsActive && o.Status != "Completed" && o.Status != "Cancelled"),
            RecentInvoices = await _context.SalesInvoices
                .Include(i => i.Customer)
                .Where(i => i.IsActive)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(5)
                .ToListAsync(),
            LowStockProducts = await _context.Products
                .Where(p => p.IsActive && p.CurrentStock <= p.ReorderLevel)
                .Take(5)
                .ToListAsync(),
            
            // Redesigned dashboard values
            SystemAlertsCount = dbAlertsCount,
            SystemAlerts = dbAlertsList
        });
    }

    [HttpGet("Help")]
    public IActionResult Help()
    {
        return View();
    }

    [HttpGet("Help/DownloadDoc")]
    public IActionResult DownloadHelpDoc()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "help", "SmartERP_User_Guide.doc");
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("User guide file not found. Please contact support.");
        }
        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, "application/msword", "SmartERP_User_Guide.doc");
    }
}
