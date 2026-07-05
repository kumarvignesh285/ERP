using ERP.Interfaces;
using ERP.Models;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace ERP.ViewModels;

public class ProductLookupViewModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("productCode")]
    public string ProductCode { get; set; } = string.Empty;
    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;
    [JsonPropertyName("salesPrice")]
    public decimal SalesPrice { get; set; }
    [JsonPropertyName("purchasePrice")]
    public decimal PurchasePrice { get; set; }
    [JsonPropertyName("gstPercentage")]
    public decimal GSTPercentage { get; set; }
    [JsonPropertyName("currentStock")]
    public decimal CurrentStock { get; set; }
    [JsonPropertyName("categoryId")]
    public int? CategoryId { get; set; }
}

public sealed class ProductEditLookupViewModel : ProductLookupViewModel
{
    [JsonPropertyName("brandId")]
    public int? BrandId { get; set; }
    [JsonPropertyName("unitId")]
    public int? UnitId { get; set; }
    [JsonPropertyName("warehouseId")]
    public int? WarehouseId { get; set; }
    [JsonPropertyName("mrp")]
    public decimal MRP { get; set; }
    [JsonPropertyName("minimumStock")]
    public decimal MinimumStock { get; set; }
    [JsonPropertyName("reorderLevel")]
    public decimal ReorderLevel { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("documentPath")]
    public string? DocumentPath { get; set; }
}

public sealed class LedgerLookupViewModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("ledgerCode")]
    public string LedgerCode { get; set; } = string.Empty;
    [JsonPropertyName("ledgerName")]
    public string LedgerName { get; set; } = string.Empty;
    [JsonPropertyName("balanceType")]
    public string BalanceType { get; set; } = string.Empty;
}

public sealed class SalesPageViewModel<TItem>
{
    public List<TItem> Items { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();
    public List<ProductLookupViewModel> ProductLookups { get; set; } = new();
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}

public sealed class PurchasePageViewModel<TItem>
{
    public List<TItem> Items { get; set; } = new();
    public List<Supplier> Suppliers { get; set; } = new();
    public List<ProductLookupViewModel> ProductLookups { get; set; } = new();
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}

public sealed class InventoryPageViewModel<TItem>
{
    public List<TItem> Items { get; set; } = new();
    public List<ProductLookupViewModel> ProductLookups { get; set; } = new();
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}

public sealed class ProductPageViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
    public List<Unit> Units { get; set; } = new();
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<ProductEditLookupViewModel> ProductEditLookups { get; set; } = new();
}

public sealed class LedgerPageViewModel
{
    public List<Ledger> Ledgers { get; set; } = new();
    public List<AccountGroup> AccountGroups { get; set; } = new();
}

public sealed class BankPageViewModel
{
    public List<Bank> Banks { get; set; } = new();
    public List<Ledger> Ledgers { get; set; } = new();
}

public sealed class VoucherPageViewModel
{
    public List<Voucher> Vouchers { get; set; } = new();
    public List<LedgerLookupViewModel> LedgerLookups { get; set; } = new();
}

public sealed class BookPageViewModel
{
    public List<VoucherItem> Items { get; set; } = new();
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
}

public sealed class AccountingReportsViewModel
{
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string BalanceSheetDate { get; set; } = string.Empty;
    public List<LedgerBalance> TrialBalance { get; set; } = new();
    public ProfitAndLossStatement ProfitAndLoss { get; set; } = new();
    public BalanceSheetStatement BalanceSheet { get; set; } = new();
}

public sealed class HomeDashboardViewModel
{
    public decimal TodaySales { get; set; }
    public decimal TodayPurchases { get; set; }
    public decimal TotalReceivables { get; set; }
    public decimal TotalPayables { get; set; }
    public decimal AvailableStockValue { get; set; }
    public int PendingApprovalsCount { get; set; }
    public int CustomerCount { get; set; }
    public int SupplierCount { get; set; }
    public int ProductCount { get; set; }
    public int OpenSalesOrders { get; set; }
    public int OpenPurchaseOrders { get; set; }
    public List<SalesInvoice> RecentInvoices { get; set; } = new();
    public List<Product> LowStockProducts { get; set; } = new();

    // Redesigned dashboard metrics
    public int SystemAlertsCount { get; set; }
    public List<Notification> SystemAlerts { get; set; } = new();
}

public sealed class UsersPageViewModel
{
    public List<AppUser> Users { get; set; } = new();
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, IList<string>> UserRoles { get; set; } = new();
}

public sealed class CrmLookupPageViewModel<TItem>
{
    public List<TItem> Items { get; set; } = new();
    public List<Lead> Leads { get; set; } = new();
}

public sealed class LoginViewModel
{
    public string ReturnUrl { get; set; } = "~/";
}
