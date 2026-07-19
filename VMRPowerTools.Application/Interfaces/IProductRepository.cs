using System.Collections.Generic;
using System.Threading.Tasks;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Application.Interfaces;

public interface IProductRepository : IRepositoryBase<Product>
{
    Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count);
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
    Task<IEnumerable<Product>> SearchProductsAsync(string query);
    Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedProductsAsync(
        int? categoryId, 
        int? brandId, 
        decimal? minPrice, 
        decimal? maxPrice, 
        string? search, 
        string? sortOrder, 
        int page, 
        int pageSize);
    Task<IEnumerable<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int count);
    Task<IEnumerable<Brand>> GetActiveBrandsAsync();
}
