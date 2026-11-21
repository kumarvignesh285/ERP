using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Interfaces;
using ERP.ViewModels;

namespace ERP.Controllers;

[Authorize]
public class AIChatController : Controller
{
    private readonly IAIChatService _chatService;
    private readonly ICompanyContext _companyContext;
    private readonly IMasterService _masterService;

    public AIChatController(
        IAIChatService chatService,
        ICompanyContext companyContext,
        IMasterService masterService)
    {
        _chatService = chatService;
        _companyContext = companyContext;
        _masterService = masterService;
    }

    // Dedicated Full-Page Business Intelligence AI Hub
    [HttpGet("/AIChat")]
    [HttpGet("/Reports/AIChat")]
    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveCompanyName = _companyContext.CurrentCompanyName ?? "All Companies (System)";
        ViewBag.ActiveCompanyId = _companyContext.CurrentCompanyId;
        ViewBag.IsSuperAdmin = _companyContext.IsSuperAdmin;
        ViewBag.Suggestions = _chatService.GetSuggestedQuestions(User);

        if (_companyContext.IsSuperAdmin)
        {
            ViewBag.Companies = await _masterService.GetAllCompaniesAsync(null, "Active");
        }

        return View();
    }

    // AJAX Endpoint for Chat Messages
    [HttpPost("/api/aichat/message")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage([FromBody] AIChatRequestDto request, CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Invalid request." });

        var result = await _chatService.ProcessMessageAsync(request, User, ct);
        return Ok(result);
    }

    // Clear Conversation History
    [HttpPost("/api/aichat/clear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearConversation([FromBody] ClearConversationDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto?.ConversationId))
        {
            await _chatService.ClearConversationHistoryAsync(dto.ConversationId, User);
        }
        return Ok(new { success = true });
    }

    // Get Suggestions for Quick Prompt Chips
    [HttpGet("/api/aichat/suggestions")]
    public IActionResult GetSuggestions()
    {
        var suggestions = _chatService.GetSuggestedQuestions(User);
        return Ok(suggestions);
    }

    // Provider Status Endpoint
    [HttpGet("/api/aichat/status")]
    public IActionResult GetStatus()
    {
        var companyName = _companyContext.CurrentCompanyName ?? "All Companies (System)";
        var companyId = _companyContext.CurrentCompanyId;

        return Ok(new
        {
            activeCompany = companyName,
            activeCompanyId = companyId,
            isSuperAdmin = _companyContext.IsSuperAdmin,
            supportedModules = new[] { "Sales", "Purchase", "Inventory", "Customers", "Suppliers", "Receivables", "Payables", "Executive BI" }
        });
    }
}

public class ClearConversationDto
{
    public string ConversationId { get; set; } = string.Empty;
}
