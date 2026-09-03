using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Filters;
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

    private bool IsAjaxRequest() =>
        Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
        Request.Headers["Accept"].ToString().Contains("application/json") ||
        Request.ContentType?.Contains("application/json") == true;

    private Dictionary<string, string> GetModelStateErrors() =>
        ModelState.Where(x => x.Value?.Errors.Count > 0)
                  .ToDictionary(
                      k => k.Key,
                      v => v.Value!.Errors.First().ErrorMessage
                  );

    // --- Leads ---
    [HttpGet("Leads")]
    [Permission("Leads", "View")]
    public async Task<IActionResult> Leads()
    {
        var list = await _crmService.GetLeadsAsync();
        return View(list);
    }

    [HttpPost("SaveLead")]
    [Permission("Leads", "Edit")]
    public async Task<IActionResult> SaveLead(Lead lead)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Please correct the validation errors.", GetModelStateErrors()));
            TempData["Error"] = "Failed to save lead. Check inputs.";
            return RedirectToAction(nameof(Leads));
        }

        try
        {
            var saved = await _crmService.SaveLeadAsync(lead);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Lead saved successfully.", saved));
            TempData["Success"] = "Lead saved successfully.";
            return RedirectToAction(nameof(Leads));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Leads));
        }
    }

    [HttpPost("DeleteLead")]
    [Permission("Leads", "Delete")]
    public async Task<IActionResult> DeleteLead(int id)
    {
        var (success, msg) = await _crmService.DeleteLeadAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }

    // --- Follow-ups ---
    [HttpGet("FollowUps")]
    [Permission("Follow Ups", "View")]
    public async Task<IActionResult> FollowUps()
    {
        return View(new CrmLookupPageViewModel<FollowUp>
        {
            Items = await _crmService.GetFollowUpsAsync(),
            Leads = await _crmService.GetLeadsAsync()
        });
    }

    [HttpPost("SaveFollowUp")]
    [Permission("Follow Ups", "Edit")]
    public async Task<IActionResult> SaveFollowUp(FollowUp followUp)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Please correct the validation errors.", GetModelStateErrors()));
            TempData["Error"] = "Failed to save follow-up.";
            return RedirectToAction(nameof(FollowUps));
        }

        try
        {
            var saved = await _crmService.SaveFollowUpAsync(followUp);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Follow-up saved successfully.", saved));
            TempData["Success"] = "Follow-up saved successfully.";
            return RedirectToAction(nameof(FollowUps));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(FollowUps));
        }
    }

    [HttpPost("DeleteFollowUp")]
    [Permission("Follow Ups", "Delete")]
    public async Task<IActionResult> DeleteFollowUp(int id)
    {
        var (success, msg) = await _crmService.DeleteFollowUpAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }

    // --- Opportunities ---
    [HttpGet("Opportunities")]
    [Permission("Opportunities", "View")]
    public async Task<IActionResult> Opportunities()
    {
        return View(new CrmLookupPageViewModel<Opportunity>
        {
            Items = await _crmService.GetOpportunitiesAsync(),
            Leads = await _crmService.GetLeadsAsync()
        });
    }

    [HttpPost("SaveOpportunity")]
    [Permission("Opportunities", "Edit")]
    public async Task<IActionResult> SaveOpportunity(Opportunity opportunity)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail("Please correct the validation errors.", GetModelStateErrors()));
            TempData["Error"] = "Failed to save opportunity.";
            return RedirectToAction(nameof(Opportunities));
        }

        try
        {
            var saved = await _crmService.SaveOpportunityAsync(opportunity);
            if (IsAjaxRequest())
                return Json(ApiResponse.Ok("Opportunity saved successfully.", saved));
            TempData["Success"] = "Opportunity saved successfully.";
            return RedirectToAction(nameof(Opportunities));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(ApiResponse.Fail(ex.Message));
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Opportunities));
        }
    }

    [HttpPost("DeleteOpportunity")]
    [Permission("Opportunities", "Delete")]
    public async Task<IActionResult> DeleteOpportunity(int id)
    {
        var (success, msg) = await _crmService.DeleteOpportunityAsync(id);
        return Json(success ? ApiResponse.Ok(msg) : ApiResponse.Fail(msg));
    }

    // --- Pipeline View ---
    [HttpGet("Pipeline")]
    [Permission("Pipeline View", "View")]
    public async Task<IActionResult> Pipeline()
    {
        var leads = await _crmService.GetLeadsAsync();
        // Return view with grouped list of leads or do sorting in Razor
        return View(leads);
    }
}
