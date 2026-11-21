using System;
using System.Collections.Generic;

namespace ERP.ViewModels;

public class AIChatRequestDto
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public List<AIChatHistoryItemDto> History { get; set; } = new();
    public int? SelectedCompanyId { get; set; }
}

public class AIChatHistoryItemDto
{
    public string Role { get; set; } = "user"; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AIChatResponseDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public string NaturalLanguageAnswer { get => Message; set => Message = value; }
    public string ConversationId { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public string? FilterApplied { get; set; }
    public string? ReportingPeriod { get; set; }
    public string? CompanyName { get; set; }
    public int? CompanyId { get; set; }
    public List<SummaryCardDto> SummaryCards { get; set; } = new();
    public ReportDataTableDto? TableData { get; set; }
    public ChartConfigDto? ChartData { get; set; }
    public List<string> SuggestedFollowUps { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string ProviderUsed { get; set; } = "OpenAI";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SummaryCardDto
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Icon { get; set; } // fontawesome class e.g. "fa-cart-shopping"
    public string ColorClass { get; set; } = "primary"; // primary, success, warning, danger, info
    public string? BadgeText { get; set; }
}

public class ReportDataTableDto
{
    public string Title { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public List<string>? FooterTotals { get; set; }
    public int TotalRecords { get; set; }
}

public class ChartConfigDto
{
    public string ChartType { get; set; } = "bar"; // bar, line, doughnut, pie
    public string Title { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public List<ChartDatasetDto> Datasets { get; set; } = new();
}

public class ChartDatasetDto
{
    public string Label { get; set; } = string.Empty;
    public List<decimal> Data { get; set; } = new();
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public List<string>? BackgroundColors { get; set; }
}

public class AIChatSuggestionDto
{
    public string Category { get; set; } = string.Empty; // Sales, Purchase, Inventory, Outstanding, BI
    public string Title { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-lightbulb";
}

public class AIChatOptions
{
    public string Provider { get; set; } = "OpenAI"; // OpenAI, Offline
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public int MaxTokens { get; set; } = 1200;
    public double Temperature { get; set; } = 0.2;
    public int RateLimitPerMinute { get; set; } = 30;
    public int MaxHistoryTurns { get; set; } = 10;
    public decimal MonthlyBudgetUsd { get; set; } = 25.0m;
}
