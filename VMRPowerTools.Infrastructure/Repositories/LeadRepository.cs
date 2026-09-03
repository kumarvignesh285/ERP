using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Domain.Entities;
using VMRPowerTools.Infrastructure.Data;

namespace VMRPowerTools.Infrastructure.Repositories;

public class LeadRepository : RepositoryBase<Lead>, ILeadRepository
{
    public LeadRepository(WebsiteDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Lead>> GetLeadsByEmailAsync(string email)
    {
        return await _dbSet
            .Include(l => l.FollowUps)
            .Where(l => l.Email == email)
            .ToListAsync();
    }
}
