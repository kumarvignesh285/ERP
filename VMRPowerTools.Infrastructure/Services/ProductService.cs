using System.Collections.Generic;
using System.Threading.Tasks;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count = 6)
    {
        return await _productRepository.GetFeaturedProductsAsync(count);
    }

    public async Task<Product?> GetProductDetailsAsync(int id)
    {
        return await _productRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
    {
        return await _productRepository.GetProductsByCategoryAsync(categoryId);
    }

    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
    {
        return await _categoryRepository.GetActiveCategoriesAsync();
    }

    public async Task<IEnumerable<Product>> SearchCatalogAsync(string query)
    {
        return await _productRepository.SearchProductsAsync(query);
    }

    public async Task<(IEnumerable<Product> Products, int TotalCount)> GetCatalogAsync(
        int? categoryId, 
        int? brandId, 
        decimal? minPrice, 
        decimal? maxPrice, 
        string? search, 
        string? sortOrder, 
        int page, 
        int pageSize)
    {
        return await _productRepository.GetPagedProductsAsync(
            categoryId, 
            brandId, 
            minPrice, 
            maxPrice, 
            search, 
            sortOrder, 
            page, 
            pageSize);
    }

    public async Task<IEnumerable<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int count = 4)
    {
        return await _productRepository.GetRelatedProductsAsync(categoryId, excludeProductId, count);
    }

    public async Task<IEnumerable<Brand>> GetActiveBrandsAsync()
    {
        return await _productRepository.GetActiveBrandsAsync();
    }
}
