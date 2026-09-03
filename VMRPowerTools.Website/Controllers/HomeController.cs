using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Website.Models;

namespace VMRPowerTools.Website.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProductService _productService;
    private readonly ILeadService _leadService;

    public HomeController(
        ILogger<HomeController> logger,
        IProductService productService,
        ILeadService leadService)
    {
        _logger = logger;
        _productService = productService;
        _leadService = leadService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var featured = await _productService.GetFeaturedProductsAsync(4);
            return View(featured);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading homepage products.");
            return View(new List<VMRPowerTools.Domain.Entities.Product>());
        }
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult Faq()
    {
        return View();
    }

    public IActionResult Terms()
    {
        return View();
    }

    public IActionResult Refund()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitInquiry(string name, string email, string phone, string message, string? companyName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Please fill in all required fields.";
                return RedirectToAction(nameof(Contact));
            }

            var success = await _leadService.SubmitInquiryAsync(name, email, phone, message, companyName);
            if (success)
            {
                TempData["Success"] = "Thank you! Your inquiry has been submitted. Our sales team will get back to you shortly.";
            }
            else
            {
                TempData["Error"] = "Failed to submit inquiry. Please try again.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting lead inquiry.");
            TempData["Error"] = "An unexpected error occurred while processing your request.";
        }

        return RedirectToAction(nameof(Contact));
    }

    [HttpGet]
    [Route("sitemap.xml")]
    public async Task<IActionResult> SitemapXml()
    {
        var products = await _productService.GetFeaturedProductsAsync(100);

        var xml = new System.Text.StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        
        xml.AppendLine("  <url>");
        xml.AppendLine($"    <loc>{Request.Scheme}://{Request.Host}/</loc>");
        xml.AppendLine("    <changefreq>daily</changefreq>");
        xml.AppendLine("    <priority>1.0</priority>");
        xml.AppendLine("  </url>");

        xml.AppendLine("  <url>");
        xml.AppendLine($"    <loc>{Request.Scheme}://{Request.Host}/Product</loc>");
        xml.AppendLine("    <changefreq>daily</changefreq>");
        xml.AppendLine("    <priority>0.9</priority>");
        xml.AppendLine("  </url>");

        xml.AppendLine("  <url>");
        xml.AppendLine($"    <loc>{Request.Scheme}://{Request.Host}/Home/About</loc>");
        xml.AppendLine("    <changefreq>monthly</changefreq>");
        xml.AppendLine("    <priority>0.7</priority>");
        xml.AppendLine("  </url>");

        xml.AppendLine("  <url>");
        xml.AppendLine($"    <loc>{Request.Scheme}://{Request.Host}/Home/Contact</loc>");
        xml.AppendLine("    <changefreq>monthly</changefreq>");
        xml.AppendLine("    <priority>0.7</priority>");
        xml.AppendLine("  </url>");

        xml.AppendLine("  <url>");
        xml.AppendLine($"    <loc>{Request.Scheme}://{Request.Host}/Home/Faq</loc>");
        xml.AppendLine("    <changefreq>monthly</changefreq>");
        xml.AppendLine("    <priority>0.6</priority>");
        xml.AppendLine("  </url>");

        foreach (var p in products)
        {
            xml.AppendLine("  <url>");
            xml.AppendLine($"    <loc>{Request.Scheme}://{Request.Host}/Product/Details/{p.Id}</loc>");
            xml.AppendLine("    <changefreq>weekly</changefreq>");
            xml.AppendLine("    <priority>0.8</priority>");
            xml.AppendLine("  </url>");
        }

        xml.AppendLine("</urlset>");

        return Content(xml.ToString(), "application/xml", System.Text.Encoding.UTF8);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
