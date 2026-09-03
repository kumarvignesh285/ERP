using System.Threading.Tasks;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Application.Interfaces;

public interface ICompanyService
{
    Task<Company?> GetDefaultCompanyAsync();
}
