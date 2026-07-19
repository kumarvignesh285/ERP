using System.Collections.Generic;
using System.Threading.Tasks;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Application.Interfaces;

public interface ICategoryRepository : IRepositoryBase<Category>
{
    Task<IEnumerable<Category>> GetActiveCategoriesAsync();
}
