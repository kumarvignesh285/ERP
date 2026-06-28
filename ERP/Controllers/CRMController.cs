using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize]
[Route("CRM")]
public class CRMController : Controller
{
    private readonly ICRMService _crmService;

    public CRMController(ICRMService crmService)
    {
        _crmService = crmService;
    }

    // --- Leads ---
    [HttpGet("Leads")]
    public async Task<IActionResult> Leads()
    {
        var list = await _crmService.GetLeadsAsync();
        return View(list);
    }

    [HttpPost("SaveLead")]
    public async Task<IActionResult> SaveLead(Lead lead)
    {
        if (ModelState.IsValid)
        {
            await _crmService.SaveLeadAsync(lead);
            TempData["Success"] = "Lead saved successfully.";
        }
        return RedirectToAction(nameof(Leads));
    }

    [HttpPost("DeleteLead")]
    public async Task<IActionResult> DeleteLead(int id)
    {
        await _crmService.DeleteLeadAsync(id);
        return Json(new { success = true });
    }

    // --- Follow-ups ---
    [HttpGet("FollowUps")]
    public async Task<IActionResult> FollowUps()
    {
        return View(new CrmLookupPageViewModel<FollowUp>
        {
            Items = await _crmService.GetFollowUpsAsync(),
            Leads = await _crmService.GetLeadsAsync()
        });
    }

    [HttpPost("SaveFollowUp")]
    public async Task<IActionResult> SaveFollowUp(FollowUp followUp)
    {
        if (ModelState.IsValid)
        {
            await _crmService.SaveFollowUpAsync(followUp);
            TempData["Success"] = "Follow-up saved successfully.";
        }
        return RedirectToAction(nameof(FollowUps));
    }

    [HttpPost("DeleteFollowUp")]
    public async Task<IActionResult> DeleteFollowUp(int id)
    {
        await _crmService.DeleteFollowUpAsync(id);
        return Json(new { success = true });
    }

    // --- Opportunities ---
    [HttpGet("Opportunities")]
    public async Task<IActionResult> Opportunities()
    {
        return View(new CrmLookupPageViewModel<Opportunity>
        {
            Items = await _crmService.GetOpportunitiesAsync(),
            Leads = await _crmService.GetLeadsAsync()
        });
    }

    [HttpPost("SaveOpportunity")]
    public async Task<IActionResult> SaveOpportunity(Opportunity opportunity)
    {
        if (ModelState.IsValid)
        {
            await _crmService.SaveOpportunityAsync(opportunity);
            TempData["Success"] = "Opportunity saved successfully.";
        }
        return RedirectToAction(nameof(Opportunities));
    }

    [HttpPost("DeleteOpportunity")]
    public async Task<IActionResult> DeleteOpportunity(int id)
    {
        await _crmService.DeleteOpportunityAsync(id);
        return Json(new { success = true });
    }

    // --- Pipeline View ---
    [HttpGet("Pipeline")]
    public async Task<IActionResult> Pipeline()
    {
        var leads = await _crmService.GetLeadsAsync();
        // Return view with grouped list of leads or do sorting in Razor
        return View(leads);
    }
}
