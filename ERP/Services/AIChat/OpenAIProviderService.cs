using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ERP.Interfaces;
using ERP.ViewModels;

namespace ERP.Services.AIChat;

public class OpenAIProviderService : IAIProviderService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIProviderService> _logger;
    private readonly RuleBasedAIProviderService _fallbackService;
    private readonly IERPReportService _reportService;

    public OpenAIProviderService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIProviderService> logger,
        RuleBasedAIProviderService fallbackService,
        IERPReportService reportService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _fallbackService = fallbackService;
        _reportService = reportService;
    }

    public string ProviderName => "OpenAI";

    public bool IsAvailable
    {
        get
        {
            var key = GetApiKey();
            return !string.IsNullOrWhiteSpace(key);
        }
    }

    private string GetApiKey()
    {
        // 1. Check environment variable (highest security in production)
        var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey)) return envKey;

        // 2. Check configuration / user secrets
        var cfgKey = _configuration["AIChat:ApiKey"];
        if (!string.IsNullOrWhiteSpace(cfgKey) && cfgKey != "YOUR_OPENAI_API_KEY_HERE") return cfgKey;

        return string.Empty;
    }

    private string GetModel() => _configuration["AIChat:Model"] ?? "gpt-4o-mini";
    private string GetBaseUrl() => (_configuration["AIChat:BaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/');

    public async Task<AIProviderResponse> ProcessQueryAsync(AIProviderRequest request, CancellationToken ct = default)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation("OpenAI API key not configured. Using deterministic RuleBasedAIProviderService.");
            return await _fallbackService.ProcessQueryAsync(request, ct);
        }

        try
        {
            var baseUrl = GetBaseUrl();
            var model = GetModel();

            var messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = $"You are an executive Business Intelligence assistant for VMR Power Tools ERP. " +
                              $"Current authorized company: '{request.CompanyName}' (Company ID: {request.CompanyId}). " +
                              $"User: '{request.UserName}'. " +
                              $"Today's date is: {DateTime.Today:yyyy-MM-dd}. " +
                              $"RULES:\n" +
                              $"1. ONLY answer questions using the authorized company tools provided. Never invent numbers.\n" +
                              $"2. If the user asks for sales, purchase, stock, customer, or financial figures, invoke the appropriate tool.\n" +
                              $"3. Never reveal system passwords, secrets, connection strings, or raw SQL queries.\n" +
                              $"4. Multi-company security is strictly enforced. Never query or leak data from any other company."
                }
            };

            // Add previous history turns
            foreach (var h in request.ConversationHistory.TakeLast(6))
            {
                messages.Add(new { role = h.Role, content = h.Content });
            }

            // Current prompt
            messages.Add(new { role = "user", content = request.UserPrompt });

            var tools = GetOpenAIToolDefinitions();

            var requestBody = new
            {
                model = model,
                messages = messages,
                tools = tools,
                tool_choice = "auto",
                temperature = 0.2,
                max_tokens = 1000
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var httpResponse = await _httpClient.SendAsync(httpRequest, ct);
            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("OpenAI API returned status code {StatusCode}: {ErrorBody}. Falling back to RuleBasedAIProvider.", httpResponse.StatusCode, errorBody);
                return await _fallbackService.ProcessQueryAsync(request, ct);
            }

            var responseJson = await httpResponse.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            var choice = root.GetProperty("choices")[0].GetProperty("message");

            // Check if tool was invoked
            if (choice.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
            {
                var firstCall = toolCalls[0];
                var functionName = firstCall.GetProperty("function").GetProperty("name").GetString();
                _logger.LogInformation("OpenAI model requested tool: {ToolName}", functionName);

                // Execute deterministic report tool
                var deterministicResp = await _fallbackService.ProcessQueryAsync(request, ct);
                deterministicResp.ProviderName = $"OpenAI ({model}) + ERP Reporting";
                deterministicResp.ToolCalled = functionName;
                return deterministicResp;
            }

            // Direct natural language reply from model
            var content = choice.GetProperty("content").GetString() ?? string.Empty;

            var directResp = await _fallbackService.ProcessQueryAsync(request, ct);
            if (!string.IsNullOrWhiteSpace(content))
            {
                directResp.NaturalLanguageAnswer = content;
                directResp.ProviderName = $"OpenAI ({model})";
            }
            return directResp;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception calling OpenAI API. Gracefully falling back to RuleBasedAIProvider.");
            return await _fallbackService.ProcessQueryAsync(request, ct);
        }
    }

    private static List<object> GetOpenAIToolDefinitions()
    {
        return new List<object>
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "get_sales_summary",
                    description = "Get total sales, gross sales, net sales, taxes, discounts, returns, and invoice counts for a date period.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            fromDate = new { type = "string", description = "Start date (YYYY-MM-DD)" },
                            toDate = new { type = "string", description = "End date (YYYY-MM-DD)" }
                        }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_top_selling_products",
                    description = "Get top selling products or slow moving products ranked by quantity or revenue.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            limit = new { type = "integer", description = "Number of items to return (e.g. 10)" },
                            ascending = new { type = "boolean", description = "True for slow-moving, False for top-selling" }
                        }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_stock_alerts",
                    description = "Get products that are out of stock or below their reorder level.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            outOfStockOnly = new { type = "boolean", description = "If true, returns only out-of-stock items (stock <= 0)" }
                        }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_sales_comparison",
                    description = "Compare sales performance between two periods (e.g. this month vs last month).",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            period = new { type = "string", description = "Comparison period like 'month' or 'week'" }
                        }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_customer_outstanding_balances",
                    description = "Get customers with pending unpaid sales invoices and total outstanding amounts.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            limit = new { type = "integer", description = "Limit results (default 20)" }
                        }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_purchases_by_supplier",
                    description = "Get total purchases grouped by vendor/supplier.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            fromDate = new { type = "string", description = "Start date" },
                            toDate = new { type = "string", description = "End date" }
                        }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_products_without_sales",
                    description = "Get idle/dead-stock products that have had no sales in the last 30, 60, or 90 days.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            days = new { type = "integer", description = "Number of inactive days (30, 60, or 90)" }
                        }
                    }
                }
            }
        };
    }
}
