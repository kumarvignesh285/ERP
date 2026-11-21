using System;
using System.Collections.Generic;

namespace ERP.ViewModels;

public class SalesSummaryResultDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalInvoices { get; set; }
    public decimal GrossSales { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalTaxes { get; set; }
    public decimal RoundOff { get; set; }
    public decimal TotalSales { get; set; } // GrandTotal
    public decimal TotalReturns { get; set; }
    public decimal NetSales => TotalSales - TotalReturns;
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal TotalQuantitySold { get; set; }
    public List<SalesByEntityDto> Breakdowns { get; set; } = new();
}

public class SalesByEntityDto
{
    public int EntityId { get; set; }
    public string EntityCode { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public int InvoiceCount { get; set; }
    public decimal Percentage { get; set; }
}

public class PurchaseSummaryResultDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalInvoices { get; set; }
    public decimal GrossPurchases { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalTaxes { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal TotalReturns { get; set; }
    public decimal NetPurchases => TotalPurchases - TotalReturns;
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal TotalQuantityPurchased { get; set; }
    public List<PurchaseByEntityDto> Breakdowns { get; set; } = new();
}

public class PurchaseByEntityDto
{
    public int EntityId { get; set; }
    public string EntityCode { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public int InvoiceCount { get; set; }
    public decimal Percentage { get; set; }
}

public class PeriodComparisonResultDto
{
    public string MetricName { get; set; } = string.Empty;
    public string Period1Label { get; set; } = "Current Period";
    public string Period2Label { get; set; } = "Previous Period";
    public decimal Period1Value { get; set; }
    public decimal Period2Value { get; set; }
    public decimal Difference => Period1Value - Period2Value;
    public decimal PercentageChange => Period2Value == 0 
        ? (Period1Value > 0 ? 100m : 0m) 
        : Math.Round(((Period1Value - Period2Value) / Math.Abs(Period2Value)) * 100m, 2);
    public string TrendDirection => Difference >= 0 ? "Up" : "Down";
    public List<PeriodComparisonItemDto> Details { get; set; } = new();
}

public class PeriodComparisonItemDto
{
    public string Label { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal Change => CurrentValue - PreviousValue;
    public decimal ChangePercent => PreviousValue == 0 ? (CurrentValue > 0 ? 100m : 0m) : Math.Round(((CurrentValue - PreviousValue) / Math.Abs(PreviousValue)) * 100m, 2);
}

public class InventorySummaryResultDto
{
    public int TotalProducts { get; set; }
    public decimal TotalStockQuantity { get; set; }
    public decimal TotalStockValuation { get; set; }
    public int InStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int NegativeStockCount { get; set; }
    public List<ProductStockItemDto> Items { get; set; } = new();
}

public class ProductStockItemDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalesPrice { get; set; }
    public decimal StockValuation => CurrentStock * PurchasePrice;
    public decimal ReorderLevel { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public string StockStatus { get; set; } = "Normal"; // Out of Stock, Low Stock, Optimal, Negative
    public decimal SuggestedReorderQty { get; set; }
}

public class OutstandingBalanceDto
{
    public int EntityId { get; set; }
    public string EntityCode { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public decimal OpeningBalance { get; set; }
    public string BalanceType { get; set; } = "Dr";
    public decimal InvoicedTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int OverdueInvoiceCount { get; set; }
    public int? CreditDays { get; set; }
    public decimal CreditLimit { get; set; }
}

public class UnpaidInvoiceDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public int OverdueDays => DueDate.HasValue ? Math.Max(0, (int)(DateTime.Today - DueDate.Value).TotalDays) : 0;
    public string Status { get; set; } = string.Empty;
}

public class AgingReportResultDto
{
    public string ReportType { get; set; } = "Receivables"; // Receivables or Payables
    public decimal CurrentDaysTotal { get; set; } // 0-30 days
    public decimal Days31To60Total { get; set; }
    public decimal Days61To90Total { get; set; }
    public decimal Above90DaysTotal { get; set; }
    public decimal TotalOutstanding => CurrentDaysTotal + Days31To60Total + Days61To90Total + Above90DaysTotal;
    public List<AgingEntityRowDto> Rows { get; set; } = new();
}

public class AgingEntityRowDto
{
    public int EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public decimal Current0To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Above90Days { get; set; }
    public decimal TotalBalance { get; set; }
}

public class ExecutiveSnapshotDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalSalesReturns { get; set; }
    public decimal NetSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal TotalPurchaseReturns { get; set; }
    public decimal NetPurchases { get; set; }
    public decimal EstimatedGrossProfit => NetSales - NetPurchases;
    public decimal OutstandingReceivables { get; set; }
    public decimal OutstandingPayables { get; set; }
    public decimal TotalStockValuation { get; set; }
    public int LowStockProductCount { get; set; }
    public int OutOfStockProductCount { get; set; }
    public int TotalActiveCustomers { get; set; }
    public int TotalActiveSuppliers { get; set; }
}

public class DailyTrendDto
{
    public DateTime Date { get; set; }
    public string DateLabel { get; set; } = string.Empty;
    public decimal SalesAmount { get; set; }
    public decimal PurchaseAmount { get; set; }
    public int SalesInvoiceCount { get; set; }
    public int PurchaseInvoiceCount { get; set; }
}
