using System.Linq;
using System.Threading.Tasks;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly IRepositoryBase<Company> _companyRepository;

    public CompanyService(IRepositoryBase<Company> companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<Company?> GetDefaultCompanyAsync()
    {
        var companies = await _companyRepository.GetAllAsync();
        return companies.FirstOrDefault(c => c.IsActive);
    }
}
