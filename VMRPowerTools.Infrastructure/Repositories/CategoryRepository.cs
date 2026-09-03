using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Domain.Entities;
using VMRPowerTools.Infrastructure.Data;

namespace VMRPowerTools.Infrastructure.Repositories;

public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
{
    public CategoryRepository(WebsiteDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .ToListAsync();
    }
}
