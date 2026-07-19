using System.Collections.Generic;
using System.Threading.Tasks;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Application.Interfaces;

public interface ILeadRepository : IRepositoryBase<Lead>
{
    Task<IEnumerable<Lead>> GetLeadsByEmailAsync(string email);
}
