using System.Threading.Tasks;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Application.Interfaces;

public interface ILeadService
{
    Task<bool> SubmitInquiryAsync(string name, string email, string phone, string message, string? companyName = null);
}
