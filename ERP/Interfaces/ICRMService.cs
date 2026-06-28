using ERP.Models;

namespace ERP.Interfaces;

public interface ICRMService
{
    // Leads
    Task<List<Lead>> GetLeadsAsync();
    Task<Lead?> GetLeadByIdAsync(int id);
    Task<Lead> SaveLeadAsync(Lead lead);
    Task DeleteLeadAsync(int id);

    // Follow-Ups
    Task<List<FollowUp>> GetFollowUpsAsync();
    Task<FollowUp?> GetFollowUpByIdAsync(int id);
    Task<FollowUp> SaveFollowUpAsync(FollowUp followUp);
    Task DeleteFollowUpAsync(int id);

    // Opportunities
    Task<List<Opportunity>> GetOpportunitiesAsync();
    Task<Opportunity?> GetOpportunityByIdAsync(int id);
    Task<Opportunity> SaveOpportunityAsync(Opportunity opportunity);
    Task DeleteOpportunityAsync(int id);
}
