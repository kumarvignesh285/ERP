using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ERP.Helpers;
using ERP.Interfaces;
using ERP.ViewModels;

namespace ERP.Services.AIChat;

public class RuleBasedAIProviderService : IAIProviderService
{
    private readonly IERPReportService _reportService;

    public RuleBasedAIProviderService(IERPReportService reportService)
    {
        _reportService = reportService;
    }

    public string ProviderName => "SmartERP BI Engine (Offline/Deterministic)";
    public bool IsAvailable => true;

    public async Task<AIProviderResponse> ProcessQueryAsync(AIProviderRequest request, CancellationToken ct = default)
    {
        var text = (request.UserPrompt ?? string.Empty).Trim();
        var lower = text.ToLowerInvariant();
        var response = new AIProviderResponse
        {
            ProviderName = ProviderName,
            ReportingPeriod = "Selected Period"
        };

        // Security / Prompt Injection & Secret leak checks
        if (Regex.IsMatch(lower, @"\b(password|secret|api[_\s]?key|connectionstring|token|hash|drop\s+table|delete\s+from|insert\s+into|update\s+\w+\s+set)\b"))
        {
            response.Success = false;
            response.ErrorMessage = "Security policy violation: Questions requesting credentials, system secrets, or data mutation commands cannot be executed.";
            response.NaturalLanguageAnswer = "I cannot fulfill this request. The SmartERP AI Assistant operates strictly in read-only business intelligence mode and adheres to multi-company security protocols. System credentials and direct SQL operations are prohibited.";
            response.Intent = "SecurityViolation";
            return response;
        }

        // Cross-company access attempt check
        if (Regex.IsMatch(lower, @"\b(another\s+company|other\s+company|all\s+companies|switch\s+to\s+company|company\s+id\s*\d+)\b") && 
            !request.UserRoles.Contains("Super Admin"))
        {
            response.Success = false;
            response.ErrorMessage = "Access Denied: You are only authorized to query business data for your assigned company.";
            response.NaturalLanguageAnswer = $"Multi-company boundary active: You are currently viewing reports strictly for **{request.CompanyName}** (ID: {request.CompanyId}). You cannot view data from other companies.";
            response.Intent = "TenantViolation";
            return response;
        }

        // 1. Comparison Queries: Sales comparison
        if ((lower.Contains("compare") || lower.Contains("comparison") || lower.Contains("vs")) && 
            (lower.Contains("sale") || lower.Contains("revenue")))
        {
            return await HandleSalesComparisonAsync(text, ct);
        }

        // 2. Comparison Queries: Purchase comparison
        if ((lower.Contains("compare") || lower.Contains("comparison") || lower.Contains("vs")) && 
            (lower.Contains("purchase") || lower.Contains("expense") || lower.Contains("procurement")))
        {
            return await HandlePurchasesComparisonAsync(text, ct);
        }

        // 3. Outstanding Balances & Dues: Customers
        if ((lower.Contains("outstanding") || lower.Contains("due") || lower.Contains("unpaid") || lower.Contains("receivable")) &&
            (lower.Contains("customer") || lower.Contains("client") || (!lower.Contains("supplier") && !lower.Contains("vendor"))))
        {
            if (lower.Contains("invoice") || lower.Contains("bill"))
                return await HandleUnpaidInvoicesAsync(ct);
            return await HandleCustomerOutstandingAsync(ct);
        }

        // 4. Outstanding Balances & Dues: Suppliers
        if ((lower.Contains("outstanding") || lower.Contains("due") || lower.Contains("unpaid") || lower.Contains("payable")) &&
            (lower.Contains("supplier") || lower.Contains("vendor")))
        {
            return await HandleSupplierOutstandingAsync(ct);
        }

        // 5. Aging Analysis
        if (lower.Contains("aging") || lower.Contains("ageing"))
        {
            if (lower.Contains("payable") || lower.Contains("supplier") || lower.Contains("vendor"))
                return await HandlePayablesAgingAsync(ct);
            return await HandleReceivablesAgingAsync(ct);
        }

        // 6. Products with no sales / Dead stock / Slow moving
        if (lower.Contains("no sales") || lower.Contains("not sold") || lower.Contains("dead stock") || lower.Contains("zero sales"))
        {
            int days = 90;
            if (lower.Contains("30 days")) days = 30;
            else if (lower.Contains("60 days")) days = 60;
            else if (lower.Contains("90 days")) days = 90;
            return await HandleProductsWithoutSalesAsync(days, ct);
        }

        // 7. Slow-moving products
        if (lower.Contains("slow moving") || lower.Contains("least selling") || lower.Contains("lowest selling") || lower.Contains("bottom"))
        {
            return await HandleTopSellingProductsAsync(text, isAscending: true, ct: ct);
        }

        // 8. Top selling products
        if (lower.Contains("top") && (lower.Contains("selling") || lower.Contains("product") || lower.Contains("item") || lower.Contains("best seller")))
        {
            return await HandleTopSellingProductsAsync(text, isAscending: false, ct: ct);
        }

        // 9. Top customers
        if (lower.Contains("top") && (lower.Contains("customer") || lower.Contains("client") || lower.Contains("buyer")))
        {
            return await HandleTopCustomersAsync(text, ct);
        }

        // 10. Top suppliers
        if (lower.Contains("top") && (lower.Contains("supplier") || lower.Contains("vendor")))
        {
            return await HandleTopSuppliersAsync(text, ct);
        }

        // 11. Out of stock & Low stock & Reorder suggestions
        if (lower.Contains("out of stock") || lower.Contains("stock out") || lower.Contains("zero stock"))
        {
            return await HandleStockAlertsAsync(outOfStockOnly: true, ct);
        }

        if (lower.Contains("low stock") || lower.Contains("shortage") || lower.Contains("minimum stock") || lower.Contains("reorder"))
        {
            return await HandleStockAlertsAsync(outOfStockOnly: false, ct);
        }

        if (lower.Contains("negative stock") || lower.Contains("minus stock"))
        {
            return await HandleNegativeStockAsync(ct);
        }

        // 12. Inventory valuation & stock breakdown
        if (lower.Contains("stock valuation") || lower.Contains("inventory value") || lower.Contains("stock value") || lower.Contains("valuation"))
        {
            return await HandleStockValuationAsync(ct);
        }

        if (lower.Contains("stock") || lower.Contains("inventory"))
        {
            return await HandleStockSummaryAsync(text, ct);
        }

        // 13. Purchases by Supplier / Product / Category
        if (lower.Contains("purchase") || lower.Contains("procurement") || lower.Contains("vendor bill"))
        {
            if (lower.Contains("supplier") || lower.Contains("vendor"))
                return await HandlePurchasesBySupplierAsync(text, ct);
            if (lower.Contains("product") || lower.Contains("item"))
                return await HandlePurchasesByProductAsync(text, ct);
            if (lower.Contains("category"))
                return await HandlePurchasesByCategoryAsync(text, ct);
            if (lower.Contains("brand"))
                return await HandlePurchasesByBrandAsync(text, ct);

            return await HandlePurchasesSummaryAsync(text, ct);
        }

        // 14. Sales by Category / Brand / Product / Customer
        if (lower.Contains("category") && (lower.Contains("sale") || lower.Contains("revenue")))
        {
            return await HandleSalesByCategoryAsync(text, ct);
        }

        if (lower.Contains("brand") && (lower.Contains("sale") || lower.Contains("revenue")))
        {
            return await HandleSalesByBrandAsync(text, ct);
        }

        // 15. Sales Trends & Declining products
        if (lower.Contains("trend") || lower.Contains("chart") || lower.Contains("growth") || lower.Contains("analytics"))
        {
            return await HandleDailyTrendsAsync(text, ct);
        }

        if (lower.Contains("declin") || lower.Contains("drop") || lower.Contains("falling"))
        {
            return await HandleDecliningProductsAsync(ct);
        }

        // 16. Executive snapshot / business summary
        if (lower.Contains("summary") || lower.Contains("snapshot") || lower.Contains("overview") || lower.Contains("kpi") || lower.Contains("health"))
        {
            return await HandleExecutiveSnapshotAsync(text, ct);
        }

        // 17. Total Sales (General Sales Query)
        if (lower.Contains("sale") || lower.Contains("revenue") || lower.Contains("billing") || lower.Contains("turnover") || lower.Contains("sold"))
        {
            return await HandleSalesSummaryAsync(text, ct);
        }

        // Unrelated query check
        if (!lower.Contains("profit") && !lower.Contains("balance") && !lower.Contains("tax") && !lower.Contains("order") && !lower.Contains("bill") && !lower.Contains("invoice"))
        {
            return new AIProviderResponse
            {
                Success = true,
                Intent = "GeneralHelp",
                NaturalLanguageAnswer = "I am the **SmartERP Business Intelligence Assistant**. I can help you analyze your company's actual business data across:\n\n" +
                    "- **Sales Analytics**: Revenue, top customers, best sellers, sales by category/brand, period comparisons.\n" +
                    "- **Purchase Insights**: Total purchases, vendor spend, top suppliers, purchase returns.\n" +
                    "- **Inventory Control**: Real-time stock levels, low-stock & out-of-stock alerts, dead stock (30/60/90 days), valuation.\n" +
                    "- **Receivables & Payables**: Unpaid customer invoices, supplier dues, aging analysis (0-30, 31-60, 61-90+ days).\n" +
                    "- **Executive BI**: Business snapshots, sales vs purchase trends, declining products.\n\n" +
                    "Try asking questions like: *\"What is my total sales this month?\"* or *\"Show my top 10 selling products\"*.",
                SuggestedFollowUps = new List<string>
                {
                    "What is my total sales this month?",
                    "Show my top 10 selling products",
                    "Which products are out of stock?",
                    "Show customer outstanding balances"
                }
            };
        }

        // Default to Sales Summary for the period
        return await HandleSalesSummaryAsync(text, ct);
    }

    #region Handler Methods

    private async Task<AIProviderResponse> HandleSalesSummaryAsync(string prompt, CancellationToken ct)
    {
        var (from, to, label) = NaturalDateHelper.ResolveDateRange(prompt);
        var summary = await _reportService.GetSalesSummaryAsync(from, to, ct: ct);

        var resp = new AIProviderResponse
        {
            Intent = "SalesSummary",
            ReportingPeriod = label,
            FilterApplied = $"Date range: {from:dd MMM yyyy} to {to:dd MMM yyyy}",
            NaturalLanguageAnswer = $"For **{label}** ({from:dd MMM yyyy} to {to:dd MMM yyyy}), your company recorded **{summary.TotalInvoices}** sales invoices. " +
                $"Gross sales totaled **₹{summary.GrossSales:N2}**, with **₹{summary.TotalTaxes:N2}** in taxes and **₹{summary.TotalDiscounts:N2}** in discounts. " +
                $"Total revenue (Grand Total) is **₹{summary.TotalSales:N2}**. Sales returns amounted to **₹{summary.TotalReturns:N2}**, resulting in a **Net Sales of ₹{summary.NetSales:N2}**. " +
                $"Received payments amount to **₹{summary.PaidAmount:N2}**, leaving a balance of **₹{summary.BalanceAmount:N2}**.",
            SummaryCards = new List<SummaryCardDto>
            {
                new() { Title = "Total Revenue", Value = $"₹{summary.TotalSales:N2}", Subtitle = $"{summary.TotalInvoices} Invoices", Icon = "fa-indian-rupee-sign", ColorClass = "primary" },
                new() { Title = "Net Sales", Value = $"₹{summary.NetSales:N2}", Subtitle = $"After ₹{summary.TotalReturns:N2} returns", Icon = "fa-receipt", ColorClass = "success" },
                new() { Title = "Units Sold", Value = summary.TotalQuantitySold.ToString("N0"), Subtitle = "Total Quantity", Icon = "fa-boxes-packing", ColorClass = "info" },
                new() { Title = "Pending Balance", Value = $"₹{summary.BalanceAmount:N2}", Subtitle = $"Paid: ₹{summary.PaidAmount:N2}", Icon = "fa-clock", ColorClass = summary.BalanceAmount > 0 ? "warning" : "secondary" }
            },
            SuggestedFollowUps = new List<string>
            {
                "Show my top 10 selling products",
                "Compare sales this month with last month",
                "Show sales by category",
                "Show customer outstanding balances"
            }
        };

        return resp;
    }

    private async Task<AIProviderResponse> HandleTopSellingProductsAsync(string prompt, bool isAscending, CancellationToken ct)
    {
        var (from, to, label) = NaturalDateHelper.ResolveDateRange(prompt);
        int limit = 10;
        var match = Regex.Match(prompt, @"\btop\s+(\d+)\b", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var parsedLimit))
            limit = Math.Clamp(parsedLimit, 3, 50);

        var items = await _reportService.GetSalesByProductAsync(from, to, limit, isAscending, ct);
        string mode = isAscending ? "Slow-Moving / Lowest-Selling" : "Top-Selling";

        var resp = new AIProviderResponse
        {
            Intent = isAscending ? "SlowMovingProducts" : "TopSellingProducts",
            ReportingPeriod = label,
            FilterApplied = $"Limit: {limit}, Period: {label}",
            SuggestedFollowUps = new List<string>
            {
                "Which products are out of stock?",
                "Show products with no sales in the last 90 days",
                "Compare sales this month with last month"
            }
        };

        if (!items.Any())
        {
            resp.NaturalLanguageAnswer = $"No product sales were recorded for **{label}**.";
            return resp;
        }

        resp.NaturalLanguageAnswer = $"Here are the **{items.Count} {mode} products** for **{label}**. The highest volume item was **{items.First().EntityName}** with **{items.First().Quantity:N0} units** sold (₹{items.First().Amount:N2}).";

        resp.TableData = new ReportDataTableDto
        {
            Title = $"{mode} Products ({label})",
            Headers = new List<string> { "Product Code", "Product Name", "Qty Sold", "Sales Amount", "% of Sales" },
            Rows = items.Select(i => new List<string>
            {
                i.EntityCode,
                i.EntityName,
                i.Quantity.ToString("N0"),
                $"₹{i.Amount:N2}",
                $"{i.Percentage:N1}%"
            }).ToList(),
            FooterTotals = new List<string>
            {
                "Total",
                $"{items.Count} Products",
                items.Sum(i => i.Quantity).ToString("N0"),
                $"₹{items.Sum(i => i.Amount):N2}",
                $"{items.Sum(i => i.Percentage):N1}%"
            },
            TotalRecords = items.Count
        };

        resp.ChartData = new ChartConfigDto
        {
            ChartType = "bar",
            Title = $"{mode} Products by Sales Amount",
            Labels = items.Take(8).Select(i => i.EntityName.Length > 16 ? i.EntityName.Substring(0, 16) + "…" : i.EntityName).ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new()
                {
                    Label = "Sales Amount (₹)",
                    Data = items.Take(8).Select(i => i.Amount).ToList(),
                    BackgroundColor = isAscending ? "#f59e0b" : "#4f46e5"
                }
            }
        };

        return resp;
    }

    private async Task<AIProviderResponse> HandleTopCustomersAsync(string prompt, CancellationToken ct)
    {
        var (from, to, label) = NaturalDateHelper.ResolveDateRange(prompt);
        int limit = 10;
        var match = Regex.Match(prompt, @"\btop\s+(\d+)\b", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var parsedLimit))
            limit = Math.Clamp(parsedLimit, 3, 50);

        var customers = await _reportService.GetSalesByCustomerAsync(from, to, limit, ct);

        var resp = new AIProviderResponse
        {
            Intent = "TopCustomers",
            ReportingPeriod = label,
            FilterApplied = $"Limit: {limit}, Period: {label}",
            SuggestedFollowUps = new List<string>
            {
                "Show customer outstanding balances",
                "Show unpaid sales invoices",
                "What is my total sales this month?"
            }
        };

        if (!customers.Any())
        {
            resp.NaturalLanguageAnswer = $"No customer sales records were found for **{label}**.";
            return resp;
        }

        resp.NaturalLanguageAnswer = $"Here are the **Top {customers.Count} Customers** for **{label}**. **{customers.First().EntityName}** generated the highest sales at **₹{customers.First().Amount:N2}** ({customers.First().Percentage:N1}% of total sales).";

        resp.TableData = new ReportDataTableDto
        {
            Title = $"Top Customers ({label})",
            Headers = new List<string> { "Code", "Customer Name", "Invoices", "Sales Amount", "% Contribution" },
            Rows = customers.Select(c => new List<string>
            {
                c.EntityCode,
                c.EntityName,
                c.InvoiceCount.ToString(),
                $"₹{c.Amount:N2}",
                $"{c.Percentage:N1}%"
            }).ToList(),
            FooterTotals = new List<string>
            {
                "Total",
                $"{customers.Count} Customers",
                customers.Sum(c => c.InvoiceCount).ToString(),
                $"₹{customers.Sum(c => c.Amount):N2}",
                $"{customers.Sum(c => c.Percentage):N1}%"
            },
            TotalRecords = customers.Count
        };

        resp.ChartData = new ChartConfigDto
        {
            ChartType = "bar",
            Title = "Top Customer Contribution",
            Labels = customers.Take(7).Select(c => c.EntityName.Length > 15 ? c.EntityName.Substring(0, 15) + "…" : c.EntityName).ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new()
                {
                    Label = "Sales Amount (₹)",
                    Data = customers.Take(7).Select(c => c.Amount).ToList(),
                    BackgroundColor = "#059669"
                }
            }
        };

        return resp;
    }

    private async Task<AIProviderResponse> HandleSalesComparisonAsync(string prompt, CancellationToken ct)
    {
        var (cFrom, cTo, pFrom, pTo, label) = NaturalDateHelper.ResolveComparisonPeriods(prompt);
        var comp = await _reportService.GetSalesComparisonAsync(cFrom, cTo, pFrom, pTo, ct);

        string trendWord = comp.Difference >= 0 ? "an increase" : "a decrease";
        string icon = comp.Difference >= 0 ? "fa-arrow-trend-up" : "fa-arrow-trend-down";
        string color = comp.Difference >= 0 ? "success" : "danger";

        var resp = new AIProviderResponse
        {
            Intent = "SalesComparison",
            ReportingPeriod = label,
            FilterApplied = $"{comp.Period1Label} vs {comp.Period2Label}",
            NaturalLanguageAnswer = $"Comparing **{comp.Period1Label}** with **{comp.Period2Label}**:\n\n" +
                $"- **Current Sales**: ₹{comp.Period1Value:N2}\n" +
                $"- **Previous Sales**: ₹{comp.Period2Value:N2}\n" +
                $"- **Change**: ₹{Math.Abs(comp.Difference):N2} ({Math.Abs(comp.PercentageChange):N1}%) representing **{trendWord}**.",
            SummaryCards = new List<SummaryCardDto>
            {
                new() { Title = "Current Sales", Value = $"₹{comp.Period1Value:N2}", Subtitle = comp.Period1Label, Icon = "fa-cart-shopping", ColorClass = "primary" },
                new() { Title = "Previous Sales", Value = $"₹{comp.Period2Value:N2}", Subtitle = comp.Period2Label, Icon = "fa-clock-rotate-left", ColorClass = "secondary" },
                new() { Title = "Difference", Value = $"{(comp.Difference >= 0 ? "+" : "-")}₹{Math.Abs(comp.Difference):N2}", Subtitle = $"{comp.PercentageChange:N1}%", Icon = icon, ColorClass = color }
            },
            TableData = new ReportDataTableDto
            {
                Title = $"Period Comparison Breakdown ({label})",
                Headers = new List<string> { "Metric", comp.Period1Label, comp.Period2Label, "Variance", "% Change" },
                Rows = comp.Details.Select(d => new List<string>
                {
                    d.Label,
                    d.Label.Contains("Invoices") || d.Label.Contains("Quantity") ? d.CurrentValue.ToString("N0") : $"₹{d.CurrentValue:N2}",
                    d.Label.Contains("Invoices") || d.Label.Contains("Quantity") ? d.PreviousValue.ToString("N0") : $"₹{d.PreviousValue:N2}",
                    d.Label.Contains("Invoices") || d.Label.Contains("Quantity") ? d.Change.ToString("N0") : $"{(d.Change >= 0 ? "+" : "")}₹{d.Change:N2}",
                    $"{d.ChangePercent:N1}%"
                }).ToList(),
                TotalRecords = comp.Details.Count
            },
            ChartData = new ChartConfigDto
            {
                ChartType = "bar",
                Title = "Sales Revenue Comparison",
                Labels = new List<string> { comp.Period2Label, comp.Period1Label },
                Datasets = new List<ChartDatasetDto>
                {
                    new()
                    {
                        Label = "Sales Amount (₹)",
                        Data = new List<decimal> { comp.Period2Value, comp.Period1Value },
                        BackgroundColors = new List<string> { "#94a3b8", comp.Difference >= 0 ? "#10b981" : "#ef4444" }
                    }
                }
            },
            SuggestedFollowUps = new List<string>
            {
                "Show my top 10 selling products",
                "Show declining sales products",
                "Compare purchases this month with last month"
            }
        };

        return resp;
    }

    private async Task<AIProviderResponse> HandlePurchasesSummaryAsync(string prompt, CancellationToken ct)
    {
        var (from, to, label) = NaturalDateHelper.ResolveDateRange(prompt);
        var summary = await _reportService.GetPurchasesSummaryAsync(from, to, ct: ct);

        return new AIProviderResponse
        {
            Intent = "PurchaseSummary",
            ReportingPeriod = label,
            FilterApplied = $"Date range: {from:dd MMM yyyy} to {to:dd MMM yyyy}",
            NaturalLanguageAnswer = $"For **{label}**, total purchase spend is **₹{summary.TotalPurchases:N2}** across **{summary.TotalInvoices}** purchase invoices. " +
                $"Purchase returns were **₹{summary.TotalReturns:N2}**, giving a **Net Purchase of ₹{summary.NetPurchases:N2}**. " +
                $"Paid amount is **₹{summary.PaidAmount:N2}**, with an outstanding balance of **₹{summary.BalanceAmount:N2}**.",
            SummaryCards = new List<SummaryCardDto>
            {
                new() { Title = "Total Purchases", Value = $"₹{summary.TotalPurchases:N2}", Subtitle = $"{summary.TotalInvoices} Bills", Icon = "fa-bag-shopping", ColorClass = "primary" },
                new() { Title = "Net Purchases", Value = $"₹{summary.NetPurchases:N2}", Subtitle = $"Returns: ₹{summary.TotalReturns:N2}", Icon = "fa-truck-ramp-box", ColorClass = "info" },
                new() { Title = "Units Purchased", Value = summary.TotalQuantityPurchased.ToString("N0"), Subtitle = "Total Qty", Icon = "fa-boxes-stacked", ColorClass = "secondary" },
                new() { Title = "Outstanding Payable", Value = $"₹{summary.BalanceAmount:N2}", Subtitle = $"Paid: ₹{summary.PaidAmount:N2}", Icon = "fa-file-invoice", ColorClass = summary.BalanceAmount > 0 ? "danger" : "success" }
            },
            SuggestedFollowUps = new List<string>
            {
                "Show purchases by supplier",
                "Show supplier outstanding balances",
                "Compare current-month purchases with the previous month"
            }
        };
    }

    private async Task<AIProviderResponse> HandlePurchasesBySupplierAsync(string prompt, CancellationToken ct)
    {
        var (from, to, label) = NaturalDateHelper.ResolveDateRange(prompt);
        var suppliers = await _reportService.GetPurchasesBySupplierAsync(from, to, 10, ct);

        var resp = new AIProviderResponse
        {
            Intent = "PurchasesBySupplier",
            ReportingPeriod = label,
            FilterApplied = $"Period: {label}",
            SuggestedFollowUps = new List<string>
            {
                "Show supplier outstanding balances",
                "Total purchases for a selected period",
                "Top suppliers by purchase value"
            }
        };

        if (!suppliers.Any())
        {
            resp.NaturalLanguageAnswer = $"No supplier purchases found for **{label}**.";
            return resp;
        }

        resp.NaturalLanguageAnswer = $"Here are the **Top Suppliers by Purchase Value** for **{label}**. **{suppliers.First().EntityName}** was the largest vendor at **₹{suppliers.First().Amount:N2}** ({suppliers.First().Percentage:N1}%).";

        resp.TableData = new ReportDataTableDto
        {
            Title = $"Purchases by Supplier ({label})",
            Headers = new List<string> { "Code", "Supplier Name", "Bills", "Purchase Value", "% Share" },
            Rows = suppliers.Select(s => new List<string>
            {
                s.EntityCode,
                s.EntityName,
                s.InvoiceCount.ToString(),
                $"₹{s.Amount:N2}",
                $"{s.Percentage:N1}%"
            }).ToList(),
            FooterTotals = new List<string>
            {
                "Total",
                $"{suppliers.Count} Suppliers",
                suppliers.Sum(s => s.InvoiceCount).ToString(),
                $"₹{suppliers.Sum(s => s.Amount):N2}",
                $"{suppliers.Sum(s => s.Percentage):N1}%"
            },
            TotalRecords = suppliers.Count
        };

        resp.ChartData = new ChartConfigDto
        {
            ChartType = "doughnut",
            Title = "Supplier Purchase Distribution",
            Labels = suppliers.Take(6).Select(s => s.EntityName).ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new()
                {
                    Label = "Purchases (₹)",
                    Data = suppliers.Take(6).Select(s => s.Amount).ToList(),
                    BackgroundColors = new List<string> { "#3b82f6", "#10b981", "#f59e0b", "#ec4899", "#8b5cf6", "#64748b" }
                }
            }
        };

        return resp;
    }

    private async Task<AIProviderResponse> HandlePurchasesByProductAsync(string prompt, CancellationToken ct)
    {
        var (from, to, label) = NaturalDateHelper.ResolveDateRange(prompt);
        var products = await _reportService.GetPurchasesByProductAsync(from, to, 10, ct);

        if (!products.Any())
        {
            return new AIProviderResponse
            {
                Intent = "PurchasesByProduct",
                ReportingPeriod = label,
                NaturalLanguageAnswer = $"No product purchase records found for **{label}**."
            };
        }

        return new AIProviderResponse
        {
            Intent = "PurchasesByProduct",
            ReportingPeriod = label,
            NaturalLanguageAnswer = $"Here are the top purchased products for **{label}**.",
            TableData = new ReportDataTableDto
            {
                Title = $"Purchased Products ({label})",
                Headers = new List<string> { "Product Code", "Product Name", "Qty", "Amount" },
                Rows = products.Select(p => new List<string> { p.EntityCode, p.EntityName, p.Quantity.ToString("N0"), $"₹{p.Amount:N2}" }).ToList(),
                TotalRecords = products.Count
            }
        };
    }

    private async Task<AIProviderResponse> HandlePurchasesByCategoryAsync(string prompt, CancellationToken ct)
    {
        var (from, to, label) = NaturalDateHelper.ResolveDateRange(prompt);
        var categories = await _reportService.GetPurchasesByCategoryAsync(from, to, ct);

        return new AIProviderResponse
        {
            Intent = "PurchasesByCategory",
            ReportingPeriod = label,
            NaturalLanguageAnswer = $"Purchases grouped by category for **{label}**.",
            TableData = new ReportDataTableDto
            {
                Title = $"Category Purchases ({label})",
                Headers = new List<string> { "Category", "Quantity", "Amount", "% Share" },
                Rows = categories.Select(c => new List<string> { c.EntityName, c.Quantity.ToString("N0"), $"₹{c.Amount:N2}", $"{c.Percentage:N1}%" }).ToList(),
                TotalRecords = categories.Count
            }
        };
    }

    private async Task<AIProviderResponse> HandlePurchasesByBrandAsync(string prompt, CancellationToken ct)
    {
        var (from, to, label) = NaturalDateHelper.ResolveDateRange(prompt);
        var brands = await _reportService.GetPurchasesByBrandAsync(from, to, ct);

        return new AIProviderResponse
        {
            Intent = "PurchasesByBrand",
            ReportingPeriod = label,
            NaturalLanguageAnswer = $"Purchases grouped by brand for **{label}**.",
            TableData = new ReportDataTableDto
            {
                Title = $"Brand Purchases ({label})",
                Headers = new List<string> { "Brand", "Quantity", "Amount", "% Share" },
                Rows = brands.Select(b => new List<string> { b.EntityName, b.Quantity.ToString("N0"), $"₹{b.Amount:N2}", $"{b.Percentage:N1}%" }).ToList(),
                TotalRecords = brands.Count
            }
        };
    }

    private async Task<AIProviderResponse> HandleTopSuppliersAsync(string prompt, CancellationToken ct)
    {
        return await HandlePurchasesBySupplierAsync(prompt, ct);
    }

    private async Task<AIProviderResponse> HandlePurchasesComparisonAsync(string prompt, CancellationToken ct)
    {
        var (cFrom, cTo, pFrom, pTo, label) = NaturalDateHelper.ResolveComparisonPeriods(prompt);
        var comp = await _reportService.GetPurchasesComparisonAsync(cFrom, cTo, pFrom, pTo, ct);

        return new AIProviderResponse
        {
            Intent = "PurchaseComparison",
            ReportingPeriod = label,
            NaturalLanguageAnswer = $"Comparing purchases in **{comp.Period1Label}** (₹{comp.Period1Value:N2}) vs **{comp.Period2Label}** (₹{comp.Period2Value:N2}): Change of ₹{Math.Abs(comp.Difference):N2} ({comp.PercentageChange:N1}%).",
            SummaryCards = new List<SummaryCardDto>
            {
                new() { Title = "Current Period", Value = $"₹{comp.Period1Value:N2}", Subtitle = comp.Period1Label, ColorClass = "primary" },
                new() { Title = "Previous Period", Value = $"₹{comp.Period2Value:N2}", Subtitle = comp.Period2Label, ColorClass = "secondary" },
                new() { Title = "Variance", Value = $"{(comp.Difference >= 0 ? "+" : "")}₹{comp.Difference:N2}", Subtitle = $"{comp.PercentageChange:N1}%", ColorClass = comp.Difference > 0 ? "warning" : "success" }
            }
        };
    }

    private async Task<AIProviderResponse> HandleStockAlertsAsync(bool outOfStockOnly, CancellationToken ct)
    {
        var items = await _reportService.GetLowAndOutOfStockProductsAsync(outOfStockOnly, 50, ct);
        string title = outOfStockOnly ? "Out-of-Stock Products" : "Low-Stock & Reorder Alerts";

        var resp = new AIProviderResponse
        {
            Intent = outOfStockOnly ? "OutOfStockProducts" : "LowStockAlerts",
            ReportingPeriod = "Current Real-Time Stock",
            SuggestedFollowUps = new List<string>
            {
                "Current stock quantity by product",
                "Inventory valuation using the ERP valuation method",
                "Show products with no sales in the last 90 days"
            }
        };

        if (!items.Any())
        {
            resp.NaturalLanguageAnswer = outOfStockOnly 
                ? "Excellent news! There are currently **no out-of-stock products** in the inventory." 
                : "All products currently maintain stock levels above their configured reorder thresholds.";
            return resp;
        }

        resp.NaturalLanguageAnswer = $"Found **{items.Count} products** that are currently **{(outOfStockOnly ? "out of stock (0 or negative)" : "at or below reorder level")}**. Prompt reordering is recommended.";

        resp.SummaryCards = new List<SummaryCardDto>
        {
            new() { Title = outOfStockOnly ? "Out of Stock" : "Alert Items", Value = items.Count.ToString(), Subtitle = "Requires Action", Icon = "fa-triangle-exclamation", ColorClass = "danger" }
        };

        resp.TableData = new ReportDataTableDto
        {
            Title = title,
            Headers = new List<string> { "Code", "Product Name", "Category", "Current Stock", "Reorder Level", "Suggested Reorder Qty" },
            Rows = items.Select(i => new List<string>
            {
                i.ProductCode,
                i.ProductName,
                i.CategoryName,
                $"{i.CurrentStock:N0} {i.UnitName}",
                $"{i.ReorderLevel:N0}",
                $"{i.SuggestedReorderQty:N0} {i.UnitName}"
            }).ToList(),
            TotalRecords = items.Count
        };

        return resp;
    }

    private async Task<AIProviderResponse> HandleNegativeStockAsync(CancellationToken ct)
    {
        var prods = await _reportService.GetNegativeStockProductsAsync(ct);
        if (!prods.Any())
        {
            return new AIProviderResponse
            {
                Intent = "NegativeStock",
                ReportingPeriod = "Current Stock",
                NaturalLanguageAnswer = "Great news! There are **no products with negative stock** in your inventory."
            };
        }

        return new AIProviderResponse
        {
            Intent = "NegativeStock",
            ReportingPeriod = "Current Stock",
            NaturalLanguageAnswer = $"Detected **{prods.Count} products with negative stock balances**. This indicates dispatches occurred without recorded stock receipts or adjustments.",
            TableData = new ReportDataTableDto
            {
                Title = "Products with Negative Stock",
                Headers = new List<string> { "Code", "Product Name", "Category", "Current Stock" },
                Rows = prods.Select(p => new List<string> { p.ProductCode, p.ProductName, p.CategoryName, $"{p.CurrentStock:N0} {p.UnitName}" }).ToList(),
                TotalRecords = prods.Count
            }
        };
    }

    private async Task<AIProviderResponse> HandleStockSummaryAsync(string prompt, CancellationToken ct)
    {
        var stock = await _reportService.GetStockSummaryAsync(ct: ct);

        return new AIProviderResponse
        {
            Intent = "StockSummary",
            ReportingPeriod = "Current Stock",
            NaturalLanguageAnswer = $"Your catalog currently holds **{stock.TotalProducts} active products** with a total physical stock quantity of **{stock.TotalStockQuantity:N0} units**. " +
                $"Total inventory valuation is **₹{stock.TotalStockValuation:N2}** (computed as Current Stock × Purchase Price). " +
                $"Stock status breakdown: **{stock.InStockCount} optimal**, **{stock.LowStockCount} low stock**, and **{stock.OutOfStockCount} out of stock**.",
            SummaryCards = new List<SummaryCardDto>
            {
                new() { Title = "Catalog Items", Value = stock.TotalProducts.ToString(), Subtitle = $"{stock.TotalStockQuantity:N0} Total Units", Icon = "fa-boxes-stacked", ColorClass = "primary" },
                new() { Title = "Stock Valuation", Value = $"₹{stock.TotalStockValuation:N2}", Subtitle = "At Purchase Price", Icon = "fa-vault", ColorClass = "success" },
                new() { Title = "Low Stock Alerts", Value = stock.LowStockCount.ToString(), Subtitle = "At or Below Reorder", Icon = "fa-triangle-exclamation", ColorClass = stock.LowStockCount > 0 ? "warning" : "secondary" },
                new() { Title = "Out of Stock", Value = stock.OutOfStockCount.ToString(), Subtitle = "Zero or Negative", Icon = "fa-circle-xmark", ColorClass = stock.OutOfStockCount > 0 ? "danger" : "secondary" }
            },
            TableData = new ReportDataTableDto
            {
                Title = "Inventory Stock Overview (Sample Items)",
                Headers = new List<string> { "Code", "Product Name", "Category", "Current Stock", "Purchase Price", "Valuation", "Status" },
                Rows = stock.Items.Take(15).Select(i => new List<string>
                {
                    i.ProductCode,
                    i.ProductName,
                    i.CategoryName,
                    $"{i.CurrentStock:N0} {i.UnitName}",
                    $"₹{i.PurchasePrice:N2}",
                    $"₹{i.StockValuation:N2}",
                    i.StockStatus
                }).ToList(),
                TotalRecords = stock.TotalProducts
            },
            SuggestedFollowUps = new List<string>
            {
                "Which products are out of stock?",
                "Inventory valuation using the ERP valuation method",
                "Show products with no sales in the last 90 days"
            }
        };
    }

    private async Task<AIProviderResponse> HandleStockValuationAsync(CancellationToken ct)
    {
        var stock = await _reportService.GetStockSummaryAsync(ct: ct);
        var byCategory = await _reportService.GetStockValuationByCategoryAsync(ct);

        return new AIProviderResponse
        {
            Intent = "StockValuation",
            ReportingPeriod = "Current Stock",
            NaturalLanguageAnswer = $"Total Inventory Valuation stands at **₹{stock.TotalStockValuation:N2}** across **{stock.TotalProducts} products**, based on FIFO/Purchase Price.",
            SummaryCards = new List<SummaryCardDto>
            {
                new() { Title = "Total Valuation", Value = $"₹{stock.TotalStockValuation:N2}", Subtitle = $"{stock.TotalStockQuantity:N0} Units", Icon = "fa-vault", ColorClass = "success" }
            },
            TableData = new ReportDataTableDto
            {
                Title = "Stock Valuation by Category",
                Headers = new List<string> { "Category", "Products", "Stock Qty", "Total Valuation" },
                Rows = byCategory.Select(c => new List<string>
                {
                    c.CategoryName,
                    c.PurchasePrice.ToString("N0"), // count
                    c.CurrentStock.ToString("N0"),
                    $"₹{c.SuggestedReorderQty:N2}" // valuation
                }).ToList(),
                TotalRecords = byCategory.Count
            },
            ChartData = new ChartConfigDto
            {
                ChartType = "doughnut",
                Title = "Stock Valuation Distribution",
                Labels = byCategory.Take(6).Select(c => c.CategoryName).ToList(),
                Datasets = new List<ChartDatasetDto>
                {
                    new()
                    {
                        Label = "Valuation (₹)",
                        Data = byCategory.Take(6).Select(c => c.SuggestedReorderQty).ToList(),
                        BackgroundColors = new List<string> { "#4f46e5", "#10b981", "#f59e0b", "#ec4899", "#8b5cf6", "#06b6d4" }
                    }
                }
            }
        };
    }

    private async Task<AIProviderResponse> HandleProductsWithoutSalesAsync(int days, CancellationToken ct)
    {
        var deadStock = await _reportService.GetProductsWithoutSalesAsync(days, 50, ct);

        var resp = new AIProviderResponse
        {
            Intent = "DeadStock",
            ReportingPeriod = $"Last {days} Days",
            FilterApplied = $"Inactivity Period: {days}+ days"
        };

        if (!deadStock.Any())
        {
            resp.NaturalLanguageAnswer = $"All active catalog products have recorded sales during the last **{days} days**! No non-moving inventory found.";
            return resp;
        }

        decimal lockedValuation = deadStock.Sum(p => p.CurrentStock * p.PurchasePrice);
        resp.NaturalLanguageAnswer = $"Identified **{deadStock.Count} products with zero sales in the last {days} days**. Total capital locked in this idle inventory is approximately **₹{lockedValuation:N2}**.";

        resp.SummaryCards = new List<SummaryCardDto>
        {
            new() { Title = "Idle Products", Value = deadStock.Count.ToString(), Subtitle = $"No sales in {days}+ days", Icon = "fa-hourglass-half", ColorClass = "warning" },
            new() { Title = "Locked Capital", Value = $"₹{lockedValuation:N2}", Subtitle = "Inventory Value", Icon = "fa-coins", ColorClass = "danger" }
        };

        resp.TableData = new ReportDataTableDto
        {
            Title = $"Non-Moving Products (Zero Sales in Last {days} Days)",
            Headers = new List<string> { "Code", "Product Name", "Category", "Current Stock", "Purchase Price", "Locked Valuation" },
            Rows = deadStock.Select(p => new List<string>
            {
                p.ProductCode,
                p.ProductName,
                p.CategoryName,
                p.CurrentStock.ToString("N0"),
                $"₹{p.PurchasePrice:N2}",
                $"₹{(p.CurrentStock * p.PurchasePrice):N2}"
            }).ToList(),
            TotalRecords = deadStock.Count
        };

        return resp;
    }

    private async Task<AIProviderResponse> HandleCustomerOutstandingAsync(CancellationToken ct)
    {
        var balances = await _reportService.GetCustomerOutstandingBalancesAsync(20, ct);

        var resp = new AIProviderResponse
        {
            Intent = "CustomerOutstanding",
            ReportingPeriod = "All Outstanding Invoices",
            SuggestedFollowUps = new List<string>
            {
                "Show unpaid sales invoices",
                "Receivables and payables aging",
                "Top 10 customers by sales amount"
            }
        };

        if (!balances.Any())
        {
            resp.NaturalLanguageAnswer = "All customers are fully paid up! There are currently **no outstanding customer balances**.";
            return resp;
        }

        decimal totalDues = balances.Sum(b => b.OutstandingAmount);
        resp.NaturalLanguageAnswer = $"Found **{balances.Count} customers** with pending receivables totaling **₹{totalDues:N2}**. **{balances.First().EntityName}** has the largest outstanding balance at **₹{balances.First().OutstandingAmount:N2}**.";

        resp.SummaryCards = new List<SummaryCardDto>
        {
            new() { Title = "Total Receivables", Value = $"₹{totalDues:N2}", Subtitle = $"{balances.Count} Customers", Icon = "fa-hand-holding-dollar", ColorClass = "warning" }
        };

        resp.TableData = new ReportDataTableDto
        {
            Title = "Customer Outstanding Balances",
            Headers = new List<string> { "Customer Code", "Customer Name", "Phone", "Total Invoiced", "Paid Amount", "Outstanding Due" },
            Rows = balances.Select(b => new List<string>
            {
                b.EntityCode,
                b.EntityName,
                b.Mobile ?? "-",
                $"₹{b.InvoicedTotal:N2}",
                $"₹{b.PaidTotal:N2}",
                $"₹{b.OutstandingAmount:N2}"
            }).ToList(),
            FooterTotals = new List<string>
            {
                "Total",
                $"{balances.Count} Customers",
                "-",
                $"₹{balances.Sum(b => b.InvoicedTotal):N2}",
                $"₹{balances.Sum(b => b.PaidTotal):N2}",
                $"₹{totalDues:N2}"
            },
            TotalRecords = balances.Count
        };

        return resp;
    }

    private async Task<AIProviderResponse> HandleSupplierOutstandingAsync(CancellationToken ct)
    {
        var balances = await _reportService.GetSupplierOutstandingBalancesAsync(20, ct);

        var resp = new AIProviderResponse
        {
            Intent = "SupplierOutstanding",
            ReportingPeriod = "All Outstanding Payables",
            SuggestedFollowUps = new List<string>
            {
                "Show customer outstanding balances",
                "Purchases by supplier",
                "Receivables and payables aging"
            }
        };

        if (!balances.Any())
        {
            resp.NaturalLanguageAnswer = "No outstanding supplier balances. All vendor bills have been cleared!";
            return resp;
        }

        decimal totalPayables = balances.Sum(b => b.OutstandingAmount);
        resp.NaturalLanguageAnswer = $"There are **{balances.Count} suppliers** with pending payables totaling **₹{totalPayables:N2}**. **{balances.First().EntityName}** has the highest pending payment at **₹{balances.First().OutstandingAmount:N2}**.";

        resp.SummaryCards = new List<SummaryCardDto>
        {
            new() { Title = "Total Payables", Value = $"₹{totalPayables:N2}", Subtitle = $"{balances.Count} Suppliers", Icon = "fa-file-invoice-dollar", ColorClass = "danger" }
        };

        resp.TableData = new ReportDataTableDto
        {
            Title = "Supplier Outstanding Dues",
            Headers = new List<string> { "Code", "Supplier Name", "Phone", "Billed Amount", "Paid Amount", "Pending Balance" },
            Rows = balances.Select(b => new List<string>
            {
                b.EntityCode,
                b.EntityName,
                b.Mobile ?? "-",
                $"₹{b.InvoicedTotal:N2}",
                $"₹{b.PaidTotal:N2}",
                $"₹{b.OutstandingAmount:N2}"
            }).ToList(),
            FooterTotals = new List<string>
            {
                "Total",
                $"{balances.Count} Suppliers",
                "-",
                $"₹{balances.Sum(b => b.InvoicedTotal):N2}",
                $"₹{balances.Sum(b => b.PaidTotal):N2}",
                $"₹{totalPayables:N2}"
            },
            TotalRecords = balances.Count
        };

        return resp;
    }

    private async Task<AIProviderResponse> HandleUnpaidInvoicesAsync(CancellationToken ct)
    {
        var invoices = await _reportService.GetUnpaidSalesInvoicesAsync(limit: 20, ct: ct);

        if (!invoices.Any())
        {
            return new AIProviderResponse
            {
                Intent = "UnpaidInvoices",
                NaturalLanguageAnswer = "All sales invoices are fully paid!"
            };
        }

        return new AIProviderResponse
        {
            Intent = "UnpaidInvoices",
            NaturalLanguageAnswer = $"Found **{invoices.Count} unpaid or partially paid sales invoices**.",
            TableData = new ReportDataTableDto
            {
                Title = "Unpaid Sales Invoices",
                Headers = new List<string> { "Invoice #", "Date", "Customer", "Grand Total", "Paid", "Balance Due", "Status" },
                Rows = invoices.Select(i => new List<string>
                {
                    i.InvoiceNumber,
                    i.InvoiceDate.ToString("yyyy-MM-dd"),
                    i.EntityName,
                    $"₹{i.GrandTotal:N2}",
                    $"₹{i.PaidAmount:N2}",
                    $"₹{i.BalanceAmount:N2}",
                    i.Status
                }).ToList(),
                TotalRecords = invoices.Count
            }
        };
    }

    private async Task<AIProviderResponse> HandleReceivablesAgingAsync(CancellationToken ct)
    {
        var aging = await _reportService.GetReceivablesAgingAsync(ct);

        return new AIProviderResponse
        {
            Intent = "ReceivablesAging",
            ReportingPeriod = "Aging Buckets",
            NaturalLanguageAnswer = $"Customer Receivables Aging Analysis:\n" +
                $"- **0-30 Days (Current)**: ₹{aging.CurrentDaysTotal:N2}\n" +
                $"- **31-60 Days**: ₹{aging.Days31To60Total:N2}\n" +
                $"- **61-90 Days**: ₹{aging.Days61To90Total:N2}\n" +
                $"- **>90 Days (Overdue)**: ₹{aging.Above90DaysTotal:N2}\n" +
                $"- **Total Outstanding**: ₹{aging.TotalOutstanding:N2}",
            SummaryCards = new List<SummaryCardDto>
            {
                new() { Title = "0-30 Days", Value = $"₹{aging.CurrentDaysTotal:N2}", ColorClass = "success" },
                new() { Title = "31-60 Days", Value = $"₹{aging.Days31To60Total:N2}", ColorClass = "info" },
                new() { Title = "61-90 Days", Value = $"₹{aging.Days61To90Total:N2}", ColorClass = "warning" },
                new() { Title = ">90 Days", Value = $"₹{aging.Above90DaysTotal:N2}", ColorClass = "danger" }
            },
            TableData = new ReportDataTableDto
            {
                Title = "Receivables Aging by Customer",
                Headers = new List<string> { "Customer Name", "0-30 Days", "31-60 Days", "61-90 Days", ">90 Days", "Total Due" },
                Rows = aging.Rows.Select(r => new List<string>
                {
                    r.EntityName,
                    $"₹{r.Current0To30:N2}",
                    $"₹{r.Days31To60:N2}",
                    $"₹{r.Days61To90:N2}",
                    $"₹{r.Above90Days:N2}",
                    $"₹{r.TotalBalance:N2}"
                }).ToList(),
                TotalRecords = aging.Rows.Count
            }
        };
    }

    private async Task<AIProviderResponse> HandlePayablesAgingAsync(CancellationToken ct)
    {
        var aging = await _reportService.GetPayablesAgingAsync(ct);

        return new AIProviderResponse
        {
            Intent = "PayablesAging",
            ReportingPeriod = "Aging Buckets",
            NaturalLanguageAnswer = $"Supplier Payables Aging Analysis:\n" +
                $"- **0-30 Days**: ₹{aging.CurrentDaysTotal:N2}\n" +
                $"- **31-60 Days**: ₹{aging.Days31To60Total:N2}\n" +
                $"- **61-90 Days**: ₹{aging.Days61To90Total:N2}\n" +
                $"- **>90 Days**: ₹{aging.Above90DaysTotal:N2}\n" +
                $"- **Total Payables**: ₹{aging.TotalOutstanding:N2}",
            TableData = new ReportDataTableDto
            {
                Title = "Payables Aging by Supplier",
                Headers = new List<string> { "Supplier Name", "0-30 Days", "31-60 Days", "61-90 Days", ">90 Days", "Total Due" },
                Rows = aging.Rows.Select(r => new List<string>
                {
                    r.EntityName,
                    $"₹{r.Current0To30:N2}",
                    $"₹{r.Days31To60:N2}",
                    $"₹{r.Days61To90:N2}",
                    $"₹{r.Above90Days:N2}",
                    $"₹{r.TotalBalance:N2}"
                }).ToList(),
                TotalRecords = aging.Rows.Count
            }
        };
    }

    private async Task<AIProviderResponse> HandleSalesByCategoryAsync(string prompt, CancellationToken ct)
    {
        var (from, to, label) = NaturalDateHelper.ResolveDateRange(prompt);
        var categories = await _reportService.GetSalesByCategoryAsync(from, to, ct);

        if (!categories.Any())
        {
            return new AIProviderResponse
            {
                Intent = "SalesByCategory",
                ReportingPeriod = label,
                NaturalLanguageAnswer = $"No categorized sales found for **{label}**."
            };
        }

        return new AIProviderResponse
        {
            Intent = "SalesByCategory",
            ReportingPeriod = label,
            NaturalLanguageAnswer = $"Sales grouped by product category for **{label}**. **{categories.First().EntityName}** produced the highest sales at **₹{categories.First().Amount:N2}** ({categories.First().Percentage:N1}%).",
            TableData = new ReportDataTableDto
            {
                Title = $"Sales by Category ({label})",
                Headers = new List<string> { "Category Name", "Quantity Sold", "Sales Value", "% Share" },
                Rows = categories.Select(c => new List<string>
                {
                    c.EntityName,
                    c.Quantity.ToString("N0"),
                    $"₹{c.Amount:N2}",
                    $"{c.Percentage:N1}%"
                }).ToList(),
                TotalRecords = categories.Count
            },
            ChartData = new ChartConfigDto
            {
                ChartType = "doughnut",
                Title = "Category Revenue Distribution",
                Labels = categories.Select(c => c.EntityName).ToList(),
                Datasets = new List<ChartDatasetDto>
                {
                    new()
                    {
                        Label = "Sales (₹)",
                        Data = categories.Select(c => c.Amount).ToList(),
                        BackgroundColors = new List<string> { "#4f46e5", "#06b6d4", "#10b981", "#f59e0b", "#ec4899", "#8b5cf6" }
                    }
                }
            }
        };
    }

    private async Task<AIProviderResponse> HandleSalesByBrandAsync(string prompt, CancellationToken ct)
    {
        var (from, to, label) = NaturalDateHelper.ResolveDateRange(prompt);
        var brands = await _reportService.GetSalesByBrandAsync(from, to, ct);

        return new AIProviderResponse
        {
            Intent = "SalesByBrand",
            ReportingPeriod = label,
            NaturalLanguageAnswer = $"Sales grouped by brand for **{label}**.",
            TableData = new ReportDataTableDto
            {
                Title = $"Sales by Brand ({label})",
                Headers = new List<string> { "Brand Name", "Quantity Sold", "Sales Value", "% Share" },
                Rows = brands.Select(b => new List<string>
                {
                    b.EntityName,
                    b.Quantity.ToString("N0"),
                    $"₹{b.Amount:N2}",
                    $"{b.Percentage:N1}%"
                }).ToList(),
                TotalRecords = brands.Count
            }
        };
    }

    private async Task<AIProviderResponse> HandleDailyTrendsAsync(string prompt, CancellationToken ct)
    {
        var (from, to, label) = NaturalDateHelper.ResolveDateRange(prompt);
        var trends = await _reportService.GetDailySalesAndPurchasesTrendAsync(from, to, ct);

        return new AIProviderResponse
        {
            Intent = "SalesAndPurchaseTrends",
            ReportingPeriod = label,
            NaturalLanguageAnswer = $"Daily trend of Sales vs Purchases for **{label}** across {trends.Count} data points.",
            ChartData = new ChartConfigDto
            {
                ChartType = "line",
                Title = $"Sales vs Purchases Trend ({label})",
                Labels = trends.Select(t => t.DateLabel).ToList(),
                Datasets = new List<ChartDatasetDto>
                {
                    new() { Label = "Sales (₹)", Data = trends.Select(t => t.SalesAmount).ToList(), BorderColor = "#4f46e5" },
                    new() { Label = "Purchases (₹)", Data = trends.Select(t => t.PurchaseAmount).ToList(), BorderColor = "#f59e0b" }
                }
            }
        };
    }

    private async Task<AIProviderResponse> HandleDecliningProductsAsync(CancellationToken ct)
    {
        var declining = await _reportService.GetDecliningSalesProductsAsync(30, 10, ct);

        if (!declining.Any())
        {
            return new AIProviderResponse
            {
                Intent = "DecliningProducts",
                NaturalLanguageAnswer = "Good news! No products with significant sales drop in the last 30 days."
            };
        }

        return new AIProviderResponse
        {
            Intent = "DecliningProducts",
            NaturalLanguageAnswer = $"Identified **{declining.Count} products with declining sales volume** compared to the prior 30-day period.",
            TableData = new ReportDataTableDto
            {
                Title = "Products with Declining Sales (Last 30 Days vs Prior 30 Days)",
                Headers = new List<string> { "Product Name", "Current Qty", "Prior Qty", "Drop in Qty" },
                Rows = declining.Select(d => new List<string>
                {
                    d.Label,
                    d.CurrentValue.ToString("N0"),
                    d.PreviousValue.ToString("N0"),
                    d.Change.ToString("N0")
                }).ToList(),
                TotalRecords = declining.Count
            }
        };
    }

    private async Task<AIProviderResponse> HandleExecutiveSnapshotAsync(string prompt, CancellationToken ct)
    {
        var (from, to, label) = NaturalDateHelper.ResolveDateRange(prompt);
        var snap = await _reportService.GetExecutiveBusinessSnapshotAsync(from, to, ct);

        return new AIProviderResponse
        {
            Intent = "ExecutiveSnapshot",
            ReportingPeriod = label,
            NaturalLanguageAnswer = $"Executive Business Snapshot for **{label}**:\n\n" +
                $"- **Net Revenue**: ₹{snap.NetSales:N2} (Grand Total: ₹{snap.TotalRevenue:N2}, Returns: ₹{snap.TotalSalesReturns:N2})\n" +
                $"- **Net Purchases**: ₹{snap.NetPurchases:N2}\n" +
                $"- **Estimated Gross Margin**: ₹{snap.EstimatedGrossProfit:N2}\n" +
                $"- **Receivables Outstanding**: ₹{snap.OutstandingReceivables:N2}\n" +
                $"- **Payables Outstanding**: ₹{snap.OutstandingPayables:N2}\n" +
                $"- **Inventory Valuation**: ₹{snap.TotalStockValuation:N2}\n" +
                $"- **Stock Health**: {snap.LowStockProductCount} low-stock alerts, {snap.OutOfStockProductCount} out-of-stock items.",
            SummaryCards = new List<SummaryCardDto>
            {
                new() { Title = "Net Sales", Value = $"₹{snap.NetSales:N2}", Subtitle = label, Icon = "fa-cart-shopping", ColorClass = "primary" },
                new() { Title = "Net Purchases", Value = $"₹{snap.NetPurchases:N2}", Subtitle = label, Icon = "fa-bag-shopping", ColorClass = "info" },
                new() { Title = "Receivables Due", Value = $"₹{snap.OutstandingReceivables:N2}", Subtitle = $"{snap.TotalActiveCustomers} Customers", Icon = "fa-hand-holding-dollar", ColorClass = "warning" },
                new() { Title = "Stock Valuation", Value = $"₹{snap.TotalStockValuation:N2}", Subtitle = $"{snap.LowStockProductCount} Low Stock", Icon = "fa-vault", ColorClass = "success" }
            },
            SuggestedFollowUps = new List<string>
            {
                "What is my total sales this month?",
                "Show my top 10 selling products",
                "Show customer outstanding balances",
                "Which products are out of stock?"
            }
        };
    }

    #endregion
}
