using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;

namespace ERP.Services;

public class CRMService : ICRMService
{
    private readonly AppDbContext _context;

    public CRMService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Lead>> GetLeadsAsync()
    {
        return await _context.Leads.Where(l => l.IsActive).ToListAsync();
    }

    public async Task<Lead?> GetLeadByIdAsync(int id)
    {
        return await _context.Leads
            .Include(l => l.FollowUps)
            .Include(l => l.Opportunities)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<Lead> SaveLeadAsync(Lead lead)
    {
        if (lead.Id == 0)
        {
            _context.Leads.Add(lead);
        }
        else
        {
            _context.Leads.Update(lead);
        }
        await _context.SaveChangesAsync();
        return lead;
    }

    public async Task DeleteLeadAsync(int id)
    {
        var item = await _context.Leads.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<FollowUp>> GetFollowUpsAsync()
    {
        return await _context.FollowUps.Include(f => f.Lead).Where(f => f.IsActive).ToListAsync();
    }

    public async Task<FollowUp?> GetFollowUpByIdAsync(int id)
    {
        return await _context.FollowUps.Include(f => f.Lead).FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<FollowUp> SaveFollowUpAsync(FollowUp followUp)
    {
        if (followUp.Id == 0)
        {
            _context.FollowUps.Add(followUp);
        }
        else
        {
            _context.FollowUps.Update(followUp);
        }
        await _context.SaveChangesAsync();
        return followUp;
    }

    public async Task DeleteFollowUpAsync(int id)
    {
        var item = await _context.FollowUps.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Opportunity>> GetOpportunitiesAsync()
    {
        return await _context.Opportunities.Include(o => o.Lead).Where(o => o.IsActive).ToListAsync();
    }

    public async Task<Opportunity?> GetOpportunityByIdAsync(int id)
    {
        return await _context.Opportunities.Include(o => o.Lead).FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Opportunity> SaveOpportunityAsync(Opportunity opportunity)
    {
        if (opportunity.Id == 0)
        {
            _context.Opportunities.Add(opportunity);
        }
        else
        {
            _context.Opportunities.Update(opportunity);
        }
        await _context.SaveChangesAsync();
        return opportunity;
    }

    public async Task DeleteOpportunityAsync(int id)
    {
        var item = await _context.Opportunities.FindAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }
}
