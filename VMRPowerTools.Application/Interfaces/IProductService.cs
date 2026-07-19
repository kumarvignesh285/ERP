using System.Collections.Generic;
using System.Threading.Tasks;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count = 6);
    Task<Product?> GetProductDetailsAsync(int id);
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
    Task<IEnumerable<Category>> GetActiveCategoriesAsync();
    Task<IEnumerable<Product>> SearchCatalogAsync(string query);
    Task<(IEnumerable<Product> Products, int TotalCount)> GetCatalogAsync(
        int? categoryId, 
        int? brandId, 
        decimal? minPrice, 
        decimal? maxPrice, 
        string? search, 
        string? sortOrder, 
        int page, 
        int pageSize);
    Task<IEnumerable<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int count = 4);
    Task<IEnumerable<Brand>> GetActiveBrandsAsync();
}
