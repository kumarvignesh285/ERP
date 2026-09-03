using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Domain.Entities;
using VMRPowerTools.Infrastructure.Data;

namespace VMRPowerTools.Infrastructure.Repositories;

public class ProductRepository : RepositoryBase<Product>, IProductRepository
{
    public ProductRepository(WebsiteDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive && p.CategoryId == categoryId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetFeaturedProductsAsync(12);
        }

        query = query.Trim().ToLower();
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive && 
                (p.ProductName.ToLower().Contains(query) || 
                 p.ProductCode.ToLower().Contains(query) || 
                 (p.Description != null && p.Description.ToLower().Contains(query))))
            .ToListAsync();
    }

    public async Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedProductsAsync(
        int? categoryId, 
        int? brandId, 
        decimal? minPrice, 
        decimal? maxPrice, 
        string? search, 
        string? sortOrder, 
        int page, 
        int pageSize)
    {
        var query = _dbSet
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (brandId.HasValue)
        {
            query = query.Where(p => p.BrandId == brandId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.SalesPrice >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.SalesPrice <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(p => p.ProductName.ToLower().Contains(cleanSearch) || 
                                 p.ProductCode.ToLower().Contains(cleanSearch) || 
                                 (p.Description != null && p.Description.ToLower().Contains(cleanSearch)));
        }

        // Apply Sorting
        query = sortOrder switch
        {
            "price_asc" => query.OrderBy(p => p.SalesPrice),
            "price_desc" => query.OrderByDescending(p => p.SalesPrice),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "name_desc" => query.OrderByDescending(p => p.ProductName),
            _ => query.OrderBy(p => p.ProductName)
        };

        var totalCount = await query.CountAsync();
        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (products, totalCount);
    }

    public async Task<IEnumerable<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int count)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive && p.CategoryId == categoryId && p.Id != excludeProductId)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Brand>> GetActiveBrandsAsync()
    {
        return await _context.Brands
            .Where(b => b.IsActive)
            .OrderBy(b => b.BrandName)
            .ToListAsync();
    }
}
