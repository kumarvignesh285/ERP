using ERP.ViewModels;
using Microsoft.AspNetCore.Hosting;

namespace ERP.Interfaces;

public interface ICompanyProvisioningService
{
    string GenerateAdminUsername(string companyCode);
    Task<string> GetNextAvailableUserNumberAsync(string companyCode);
    Task<bool> IsUsernameAvailableAsync(string username);
    Task<CompanyProvisioningResult> ProvisionCompanyAsync(CompanyProvisioningViewModel model, IWebHostEnvironment env, string currentUserId);
}
