using System;
using System.Collections.Generic;
using ERP.Models;

namespace ERP.ViewModels;

public class DashboardDateFilterDto
{
    public string Period { get; set; } = "ThisMonth"; // Today, Yesterday, ThisWeek, ThisMonth, LastMonth, ThisYear, Custom
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class CompanySummaryRowDto
{
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SuperAdminDashboardViewModel
{
    // Company Stats
    public int TotalCompanies { get; set; }
    public int ActiveCompanies { get; set; }
    public int InactiveCompanies { get; set; }

    // User & Security Stats
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalRoles { get; set; }
    public int SuccessfulLoginsToday { get; set; }
    public int FailedLoginsToday { get; set; }
    public int ActiveSessions { get; set; }

    // Platform-Wide Business Aggregations
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalSuppliers { get; set; }
    public decimal TotalPlatformSales { get; set; }
    public decimal TotalPlatformPurchases { get; set; }

    // Company Performance Matrix
    public List<CompanySummaryRowDto> Companies { get; set; } = new();

    // Global Activity Stream
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
}

public class RecentSalesInvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RecentPurchaseInvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class LowStockProductDto
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal MinimumStock { get; set; }
    public string UnitName { get; set; } = string.Empty;
}

public class RecentActivityDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? CompanyCode { get; set; }
    public string Severity { get; set; } = "Info";
    public DateTime Timestamp { get; set; }
}

public class DashboardChartDataDto
{
    public List<string> TrendLabels { get; set; } = new();
    public List<decimal> SalesTrendValues { get; set; } = new();
    public List<decimal> PurchaseTrendValues { get; set; } = new();
    public List<string> CategoryLabels { get; set; } = new();
    public List<decimal> CategoryStockValues { get; set; } = new();
}

public class CompanyAdminDashboardViewModel
{
    // Company Information
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;

    // Date Filter Context
    public DashboardDateFilterDto Filter { get; set; } = new();
    public DateTime FilterFrom { get; set; }
    public DateTime FilterTo { get; set; }

    // Trading & Financial Metrics
    public decimal TodaySales { get; set; }
    public decimal PeriodSales { get; set; }
    public int PeriodSalesCount { get; set; }
    public decimal TotalReceivables { get; set; }

    public decimal TodayPurchases { get; set; }
    public decimal PeriodPurchases { get; set; }
    public int PeriodPurchasesCount { get; set; }
    public decimal TotalPayables { get; set; }

    public decimal TodayExpenses { get; set; }
    public decimal PeriodExpenses { get; set; }

    // Financial Summary
    public decimal Revenue { get; set; }
    public decimal PurchasesCost { get; set; }
    public decimal TotalExpensesSum { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetMarginPercentage { get; set; }

    // Master & Inventory Metrics
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int TotalSuppliers { get; set; }
    public int ActiveSuppliers { get; set; }
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public decimal AvailableStockValue { get; set; }
    public int TotalEmployees { get; set; }

    // Orders & Approvals
    public int OpenSalesOrders { get; set; }
    public int OpenPurchaseOrders { get; set; }
    public int PendingApprovalsCount { get; set; }

    // Security & Login Activity
    public int SuccessfulLoginsToday { get; set; }
    public int FailedLoginsToday { get; set; }
    public int ActiveSessions { get; set; }

    // Tabular Lists
    public List<RecentSalesInvoiceDto> RecentSales { get; set; } = new();
    public List<RecentPurchaseInvoiceDto> RecentPurchases { get; set; } = new();
    public List<LowStockProductDto> LowStockProducts { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();

    // Chart Data
    public DashboardChartDataDto Charts { get; set; } = new();

    // Permissions for conditional rendering
    public bool CanViewSales { get; set; } = true;
    public bool CanCreateSales { get; set; } = true;
    public bool CanViewPurchases { get; set; } = true;
    public bool CanCreatePurchases { get; set; } = true;
    public bool CanViewInventory { get; set; } = true;
    public bool CanViewAccounts { get; set; } = true;
    public bool CanViewReports { get; set; } = true;
    public bool CanViewAudit { get; set; } = true;
}

public class UnifiedDashboardViewModel
{
    public bool IsSuperAdminDashboard { get; set; }
    public bool IsSuperAdminUser { get; set; }
    public int? ActiveCompanyId { get; set; }
    public string? ActiveCompanyName { get; set; }
    public string? ActiveCompanyCode { get; set; }

    public DashboardDateFilterDto Filter { get; set; } = new();

    public SuperAdminDashboardViewModel? SuperAdminModel { get; set; }
    public CompanyAdminDashboardViewModel? CompanyAdminModel { get; set; }
}
