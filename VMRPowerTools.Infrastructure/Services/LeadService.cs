using System;
using System.Threading.Tasks;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Infrastructure.Services;

public class LeadService : ILeadService
{
    private readonly ILeadRepository _leadRepository;

    public LeadService(ILeadRepository leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public async Task<bool> SubmitInquiryAsync(string name, string email, string phone, string message, string? companyName = null)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var lead = new Lead
        {
            LeadName = name,
            Email = email,
            Phone = phone,
            Remarks = message,
            CompanyName = companyName,
            Source = "Web",
            Status = "New",
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        // Create an initial follow-up task
        var followup = new FollowUp
        {
            FollowUpDate = DateTime.Now,
            FollowUpType = "Email",
            Remarks = $"New web inquiry received from website: '{message[..(message.Length > 200 ? 200 : message.Length)]}...'",
            Status = "Pending",
            NextFollowUpDate = DateTime.Today.AddDays(1),
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        lead.FollowUps.Add(followup);

        await _leadRepository.AddAsync(lead);
        await _leadRepository.SaveChangesAsync();

        return true;
    }
}
