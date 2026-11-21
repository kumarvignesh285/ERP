using System;
using System.Collections.Generic;
using ERP.ViewModels;

namespace ERP.Services.AIChat;

public class AIProviderRequest
{
    public string UserPrompt { get; set; } = string.Empty;
    public string SystemInstructions { get; set; } = string.Empty;
    public List<AIChatHistoryItemDto> ConversationHistory { get; set; } = new();
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<string> UserRoles { get; set; } = new();
}

public class AIProviderResponse
{
    public bool Success { get; set; } = true;
    public string NaturalLanguageAnswer { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public string? ToolCalled { get; set; }
    public string? FilterApplied { get; set; }
    public string? ReportingPeriod { get; set; }
    public List<SummaryCardDto> SummaryCards { get; set; } = new();
    public ReportDataTableDto? TableData { get; set; }
    public ChartConfigDto? ChartData { get; set; }
    public List<string> SuggestedFollowUps { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string ProviderName { get; set; } = "SmartERP BI Engine (Offline/Deterministic)";
}
