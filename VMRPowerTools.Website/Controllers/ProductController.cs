using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Domain.Entities;
using VMRPowerTools.Website.Models;

namespace VMRPowerTools.Website.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductController> _logger;
    private const string RecentlyViewedCookieName = "VMR_RecentlyViewed";

    public ProductController(IProductService productService, ILogger<ProductController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(
        int? categoryId, 
        int? brandId, 
        decimal? minPrice, 
        decimal? maxPrice, 
        string? search, 
        string? sortOrder, 
        int page = 1)
    {
        try
        {
            int pageSize = 9; // Grid of 3x3 layout looks excellent on desktop

            var (products, totalCount) = await _productService.GetCatalogAsync(
                categoryId, brandId, minPrice, maxPrice, search, sortOrder, page, pageSize);

            var viewModel = new ProductCatalogViewModel
            {
                Products = products,
                Categories = await _productService.GetActiveCategoriesAsync(),
                Brands = await _productService.GetActiveBrandsAsync(),
                CategoryId = categoryId,
                BrandId = brandId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Search = search,
                SortOrder = sortOrder,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalCount
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering catalog index page.");
            return View(new ProductCatalogViewModel());
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var product = await _productService.GetProductDetailsAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // 1. Fetch Related Products (same category, excluding this product)
            if (product.CategoryId.HasValue)
            {
                ViewBag.RelatedProducts = await _productService.GetRelatedProductsAsync(
                    product.CategoryId.Value, product.Id, count: 4);
            }
            else
            {
                ViewBag.RelatedProducts = new List<Product>();
            }

            // 2. Manage Recently Viewed products from Cookies
            var recentlyViewedIds = GetRecentlyViewedIdsFromCookie();
            
            // Load details for recently viewed items (excluding current)
            var viewedProducts = new List<Product>();
            foreach (var viewedId in recentlyViewedIds.Where(x => x != product.Id))
            {
                var p = await _productService.GetProductDetailsAsync(viewedId);
                if (p != null)
                {
                    viewedProducts.Add(p);
                }
            }
            ViewBag.RecentlyViewed = viewedProducts;

            // Add current product to viewed list cookie
            AddProductToRecentlyViewedCookie(product.Id);

            return View(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering product details for Id {ProductId}.", id);
            return RedirectToAction(nameof(Index));
        }
    }

    #region Cookie Helpers
    private List<int> GetRecentlyViewedIdsFromCookie()
    {
        var ids = new List<int>();
        var cookieValue = Request.Cookies[RecentlyViewedCookieName];
        if (!string.IsNullOrEmpty(cookieValue))
        {
            ids = cookieValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(x => int.TryParse(x, out var parsed) ? parsed : 0)
                             .Where(x => x > 0)
                             .ToList();
        }
        return ids;
    }

    private void AddProductToRecentlyViewedCookie(int productId)
    {
        var ids = GetRecentlyViewedIdsFromCookie();
        
        // Remove duplicate if it already exists, then insert at index 0 (newest first)
        ids.Remove(productId);
        ids.Insert(0, productId);

        // Keep maximum of 5 recently viewed products
        var updatedValue = string.Join(",", ids.Take(5));
        
        var options = new CookieOptions
        {
            Expires = DateTime.Now.AddDays(7),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        };
        Response.Cookies.Append(RecentlyViewedCookieName, updatedValue, options);
    }
    #endregion
}
