using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;

namespace ERP.Services;

public class CompanySampleDataService : ICompanySampleDataService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CompanySampleDataService> _logger;

    public CompanySampleDataService(AppDbContext context, ILogger<CompanySampleDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasSampleDataAsync(int companyId)
    {
        return false;
    }

    public async Task<SampleDataInitResult> InitializeSampleDataAsync(int companyId, string createdBy)
    {
        var result = new SampleDataInitResult();

        if (companyId <= 0)
        {
            result.Success = false;
            result.Message = "Invalid Company ID.";
            return result;
        }

        var company = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == companyId);
        if (company == null)
        {
            result.Success = false;
            result.Message = $"Company with ID {companyId} does not exist.";
            return result;
        }

        result.Success = true;
        result.Message = $"Company '{company.CompanyName}' initialized cleanly with zero sample data.";
        return result;
    }
}
