using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ERP.Interfaces;
using ERP.Services.AIChat;
using ERP.ViewModels;

namespace ERP.Services;

public class AIChatService : IAIChatService
{
    private readonly ICompanyContext _companyContext;
    private readonly IPermissionService _permissionService;
    private readonly OpenAIProviderService _aiProvider;
    private readonly ILogger<AIChatService> _logger;

    // In-memory sliding window rate limiter: Key = UserId, Value = Queue of timestamps
    private static readonly ConcurrentDictionary<string, List<DateTime>> _rateLimits = new();
    private static readonly ConcurrentDictionary<string, List<AIChatHistoryItemDto>> _conversationStore = new();

    public AIChatService(
        ICompanyContext companyContext,
        IPermissionService permissionService,
        OpenAIProviderService aiProvider,
        ILogger<AIChatService> logger)
    {
        _companyContext = companyContext;
        _permissionService = permissionService;
        _aiProvider = aiProvider;
        _logger = logger;
    }

    public async Task<AIChatResponseDto> ProcessMessageAsync(AIChatRequestDto request, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var response = new AIChatResponseDto
        {
            ConversationId = string.IsNullOrWhiteSpace(request.ConversationId) ? Guid.NewGuid().ToString("N") : request.ConversationId
        };

        try
        {
            // 1. Authentication check
            if (user == null || user.Identity?.IsAuthenticated != true)
            {
                response.Success = false;
                response.ErrorMessage = "Authentication required. Please sign in to access the SmartERP AI Assistant.";
                response.NaturalLanguageAnswer = "You must be signed in to access company reports and business intelligence.";
                return response;
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "anonymous";
            var userName = user.Identity?.Name ?? "User";

            // 2. Rate limiting check (Max 30 requests per minute per user)
            if (IsRateLimited(userId, maxRequestsPerMinute: 30))
            {
                response.Success = false;
                response.ErrorMessage = "Rate limit reached. Please wait a moment before sending another question.";
                response.NaturalLanguageAnswer = "You have reached the temporary question limit (30 requests/minute). Please wait 30 seconds and try again.";
                return response;
            }

            // 3. Multi-Company Tenant Security Check
            int? activeCompanyId = _companyContext.CurrentCompanyId;
            string activeCompanyName = _companyContext.CurrentCompanyName ?? "All Companies (System)";

            // Check if Super Admin provided a SelectedCompanyId override
            if (_companyContext.IsSuperAdmin && request.SelectedCompanyId.HasValue && request.SelectedCompanyId.Value > 0)
            {
                activeCompanyId = request.SelectedCompanyId.Value;
            }

            response.CompanyId = activeCompanyId;
            response.CompanyName = activeCompanyName;

            var userPrompt = (request.Message ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                response.Success = false;
                response.ErrorMessage = "Please type a question or select a prompt.";
                response.NaturalLanguageAnswer = "How can I assist you with your business reports today?";
                return response;
            }

            // 4. Role-based Permission Pre-Validation
            var authCheck = await ValidateUserModuleAccessAsync(user, userPrompt);
            if (!authCheck.IsAllowed)
            {
                response.Success = false;
                response.ErrorMessage = authCheck.DeniedReason;
                response.NaturalLanguageAnswer = authCheck.DeniedReason ?? "Access Denied";
                response.Intent = "AccessDenied";
                return response;
            }

            // 5. Build conversation history
            var convKey = $"{userId}_{response.ConversationId}";
            var history = _conversationStore.GetOrAdd(convKey, _ => new List<AIChatHistoryItemDto>());

            // Add client-sent history if available
            if (request.History != null && request.History.Any())
            {
                history = request.History.TakeLast(8).ToList();
            }

            var userRoles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            var providerRequest = new AIProviderRequest
            {
                UserPrompt = userPrompt,
                ConversationHistory = history,
                CompanyId = activeCompanyId ?? 0,
                CompanyName = activeCompanyName,
                UserName = userName,
                UserRoles = userRoles
            };

            // 6. Invoke AI Provider
            var providerResponse = await _aiProvider.ProcessQueryAsync(providerRequest, ct);

            // 7. Format final response
            response.Success = providerResponse.Success;
            response.Message = providerResponse.NaturalLanguageAnswer;
            response.NaturalLanguageAnswer = providerResponse.NaturalLanguageAnswer;
            response.Intent = providerResponse.Intent;
            response.FilterApplied = providerResponse.FilterApplied;
            response.ReportingPeriod = providerResponse.ReportingPeriod;
            response.SummaryCards = providerResponse.SummaryCards;
            response.TableData = providerResponse.TableData;
            response.ChartData = providerResponse.ChartData;
            response.SuggestedFollowUps = providerResponse.SuggestedFollowUps;
            response.ErrorMessage = providerResponse.ErrorMessage;
            response.ProviderUsed = providerResponse.ProviderName;

            // 8. Record turn into history
            history.Add(new AIChatHistoryItemDto { Role = "user", Content = userPrompt, Timestamp = DateTime.UtcNow });
            history.Add(new AIChatHistoryItemDto { Role = "assistant", Content = response.Message, Timestamp = DateTime.UtcNow });
            if (history.Count > 16) history.RemoveRange(0, history.Count - 16);
            _conversationStore[convKey] = history;

            sw.Stop();
            _logger.LogInformation("AIChat query processed for user {User} in company {CompanyId} ({ElapsedMs}ms, Provider: {Provider})",
                userId, activeCompanyId, sw.ElapsedMilliseconds, response.ProviderUsed);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error processing AI Chat query for user {User}", user?.Identity?.Name);
            response.Success = false;
            response.ErrorMessage = "An internal reporting error occurred while querying ERP data. Please try again or refine your query.";
            response.NaturalLanguageAnswer = "I encountered an error querying the ERP database. Please verify your date range or filters and try again.";
            return response;
        }
    }

    public List<AIChatSuggestionDto> GetSuggestedQuestions(ClaimsPrincipal user)
    {
        var suggestions = new List<AIChatSuggestionDto>
        {
            // Sales
            new() { Category = "Sales", Title = "Total Sales This Month", Query = "What is my total sales this month?", Icon = "fa-cart-shopping" },
            new() { Category = "Sales", Title = "Top 10 Selling Products", Query = "Show my top 10 selling products", Icon = "fa-fire" },
            new() { Category = "Sales", Title = "Sales by Category", Query = "Show sales grouped by category", Icon = "fa-chart-pie" },
            new() { Category = "Sales", Title = "Month-over-Month Comparison", Query = "Compare sales this month with last month", Icon = "fa-arrow-trend-up" },

            // Purchase
            new() { Category = "Purchase", Title = "Total Purchases", Query = "Show total purchases for this month", Icon = "fa-bag-shopping" },
            new() { Category = "Purchase", Title = "Purchases by Supplier", Query = "Show purchases by supplier", Icon = "fa-truck" },
            new() { Category = "Purchase", Title = "Supplier Outstanding Dues", Query = "Show supplier outstanding balances", Icon = "fa-file-invoice-dollar" },

            // Inventory
            new() { Category = "Inventory", Title = "Out-of-Stock Alert", Query = "Which products are out of stock?", Icon = "fa-triangle-exclamation" },
            new() { Category = "Inventory", Title = "Low Stock Reorder List", Query = "Show low stock products and reorder suggestions", Icon = "fa-boxes-packing" },
            new() { Category = "Inventory", Title = "Stock Valuation", Query = "Show total inventory valuation by category", Icon = "fa-vault" },
            new() { Category = "Inventory", Title = "Dead Stock (90 Days)", Query = "Show products with no sales in the last 90 days", Icon = "fa-hourglass-half" },

            // Outstanding
            new() { Category = "Receivables", Title = "Customer Outstanding", Query = "Show customer outstanding balances", Icon = "fa-hand-holding-dollar" },
            new() { Category = "Receivables", Title = "Unpaid Sales Invoices", Query = "Find unpaid sales invoices", Icon = "fa-clock" },
            new() { Category = "Aging", Title = "Receivables Aging", Query = "Show receivables aging analysis", Icon = "fa-calendar-days" },

            // Executive BI
            new() { Category = "Executive BI", Title = "Business Health Snapshot", Query = "Summarize the most important business figures this month", Icon = "fa-gauge-high" },
            new() { Category = "Executive BI", Title = "Sales vs Purchase Trends", Query = "Compare sales and purchases daily trends", Icon = "fa-chart-line" }
        };

        return suggestions;
    }

    public Task ClearConversationHistoryAsync(string conversationId, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "anonymous";
        var convKey = $"{userId}_{conversationId}";
        _conversationStore.TryRemove(convKey, out _);
        return Task.CompletedTask;
    }

    private static bool IsRateLimited(string userId, int maxRequestsPerMinute)
    {
        var now = DateTime.UtcNow;
        var timestamps = _rateLimits.GetOrAdd(userId, _ => new List<DateTime>());

        lock (timestamps)
        {
            timestamps.RemoveAll(t => (now - t).TotalMinutes >= 1);
            if (timestamps.Count >= maxRequestsPerMinute)
            {
                return true;
            }
            timestamps.Add(now);
            return false;
        }
    }

    private async Task<(bool IsAllowed, string? DeniedReason)> ValidateUserModuleAccessAsync(ClaimsPrincipal user, string prompt)
    {
        var lower = prompt.ToLowerInvariant();

        // Super Admin, Company Admin, and Admin have comprehensive access
        if (user.IsInRole("Super Admin") || user.IsInRole("Admin") || user.IsInRole("Company Admin") || user.IsInRole("CompanyAdmin"))
        {
            return (true, null);
        }

        // Check Financial / Accounting questions
        if (Regex.IsMatch(lower, @"\b(profit|loss|pnl|trial\s+balance|balance\s+sheet|voucher|general\s+ledger|cash\s+book|bank\s+book)\b"))
        {
            var canViewAccounts = await _permissionService.HasPermissionAsync(user, "Accounting Reports", "View");
            if (!canViewAccounts)
            {
                return (false, "Access Denied: Your account role does not have permission to view Accounting Reports and financial statements.");
            }
        }

        // Check Purchase questions for users with restricted roles
        if (Regex.IsMatch(lower, @"\b(purchase|supplier|vendor|payables\s+aging|procurement)\b") && !lower.Contains("sale"))
        {
            var canViewPurchases = await _permissionService.HasPermissionAsync(user, "Purchase Reports", "View");
            if (!canViewPurchases)
            {
                return (false, "Access Denied: Your account does not have permission to view Purchase and Supplier Reports.");
            }
        }

        // Check Sales questions
        if (Regex.IsMatch(lower, @"\b(sales?|revenue|receivables|customer|invoices?)\b") && !lower.Contains("purchase"))
        {
            var canViewSales = await _permissionService.HasPermissionAsync(user, "Sales Reports", "View");
            if (!canViewSales)
            {
                return (false, "Access Denied: Your account does not have permission to view Sales Reports.");
            }
        }

        return (true, null);
    }
}
