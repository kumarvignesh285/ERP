using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ERP.Filters;
using ERP.Helpers;
using ERP.Interfaces;
using ERP.Interfaces.Reporting;
using ERP.ViewModels.Reporting;

namespace ERP.Controllers;

[Authorize]
[Permission("Reports", "View")]
[Route("Reports")]
public class ReportsController : Controller
{
    private readonly IReportDashboardService _dashboardService;
    private readonly ISalesReportService _salesReportService;
    private readonly IPurchaseReportService _purchaseReportService;
    private readonly IInventoryReportService _inventoryReportService;
    private readonly IAccountsReportService _accountsReportService;
    private readonly IGstReportService _gstReportService;
    private readonly ICustomerSupplierReportService _custSuppReportService;
    private readonly IAuditReportService _auditReportService;
    private readonly IBusinessAnalyticsService _biService;
    private readonly IReportExportService _exportService;
    private readonly IMasterService _masterService;
    private readonly ICompanyContext _companyContext;

    public ReportsController(
        IReportDashboardService dashboardService,
        ISalesReportService salesReportService,
        IPurchaseReportService purchaseReportService,
        IInventoryReportService inventoryReportService,
        IAccountsReportService accountsReportService,
        IGstReportService gstReportService,
        ICustomerSupplierReportService custSuppReportService,
        IAuditReportService auditReportService,
        IBusinessAnalyticsService biService,
        IReportExportService exportService,
        IMasterService masterService,
        ICompanyContext companyContext)
    {
        _dashboardService = dashboardService;
        _salesReportService = salesReportService;
        _purchaseReportService = purchaseReportService;
        _inventoryReportService = inventoryReportService;
        _accountsReportService = accountsReportService;
        _gstReportService = gstReportService;
        _custSuppReportService = custSuppReportService;
        _auditReportService = auditReportService;
        _biService = biService;
        _exportService = exportService;
        _masterService = masterService;
        _companyContext = companyContext;
    }

    private string ActiveCompanyName => _companyContext.CurrentCompanyName ?? "VMR Qubrix ERP";
    private string ActiveCompanyCode => _companyContext.CurrentCompanyCode ?? "VMR";

    private async Task PopulateFilterLookups()
    {
        ViewBag.Customers = await _masterService.GetCustomersAsync();
        ViewBag.Suppliers = await _masterService.GetSuppliersAsync();
        ViewBag.Products = await _masterService.GetProductsAsync();
        ViewBag.Categories = await _masterService.GetCategoriesAsync();
        ViewBag.Brands = await _masterService.GetBrandsAsync();
        ViewBag.Warehouses = await _masterService.GetWarehousesAsync();
    }

    // --- 1. Reports Dashboard (Index) ---
    [HttpGet("")]
    [HttpGet("Index")]
    [HttpGet("Dashboard")]
    public async Task<IActionResult> Index([FromQuery] ReportFilterViewModel filter, CancellationToken ct = default)
    {
        var sessionMode = HttpContext.Session.GetString("ReportingMode");
        if (!string.IsNullOrEmpty(filter.Mode))
        {
            HttpContext.Session.SetString("ReportingMode", filter.Mode);
        }
        else if (!string.IsNullOrEmpty(sessionMode))
        {
            filter.Mode = sessionMode;
        }

        ViewBag.ReportingMode = filter.Mode;

        if (filter.Mode == "Simple")
        {
            var simpleData = await _dashboardService.GetSimpleModeDashboardDataAsync(ct);
            return View("SimpleDashboard", simpleData);
        }
        else
        {
            var advancedData = await _dashboardService.GetAdvancedDashboardDataAsync(filter, ct);
            ViewBag.Filter = filter;
            return View("Dashboard", advancedData);
        }
    }

    // --- 2. Sales Reports ---
    [HttpGet("Sales")]
    [HttpGet("SalesReports")]
    public async Task<IActionResult> Sales([FromQuery] ReportFilterViewModel filter, CancellationToken ct = default)
    {
        await PopulateFilterLookups();
        if (string.IsNullOrWhiteSpace(filter.ReportType)) filter.ReportType = "DailySales";

        var vm = new ReportResultViewModel<object>
        {
            ReportType = filter.ReportType,
            Category = "Sales",
            Filter = filter,
            ActiveCompanyName = ActiveCompanyName,
            ActiveCompanyCode = ActiveCompanyCode
        };

        switch (filter.ReportType)
        {
            case "MonthlySales":
                vm.ReportTitle = "Monthly Sales Report";
                vm.ReportDescription = "Month-over-month sales performance, tax collection, and revenue progression.";
                vm.Data = (await _salesReportService.GetMonthlySalesReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "YearlySales":
                vm.ReportTitle = "Yearly Sales Report";
                vm.ReportDescription = "Annual revenue summary and sales returns comparison across financial years.";
                vm.Data = (await _salesReportService.GetYearlySalesReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "SalesRegister":
                vm.ReportTitle = "Sales Register";
                vm.ReportDescription = "Comprehensive register of all sales invoices with customer, tax, and status breakdown.";
                vm.Data = (await _salesReportService.GetSalesRegisterAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "ProductWise":
                vm.ReportTitle = "Product-wise Sales Report";
                vm.ReportDescription = "Volume sold, revenue contribution, and average selling rate per product.";
                vm.Data = (await _salesReportService.GetProductWiseSalesAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "CategoryWise":
                vm.ReportTitle = "Category-wise Sales Report";
                vm.ReportDescription = "Revenue distribution and quantity sold grouped by product category.";
                vm.Data = (await _salesReportService.GetCategoryWiseSalesAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "BrandWise":
                vm.ReportTitle = "Brand-wise Sales Report";
                vm.ReportDescription = "Sales performance and product turnover broken down by manufacturer / brand.";
                vm.Data = (await _salesReportService.GetBrandWiseSalesAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "CustomerWise":
                vm.ReportTitle = "Customer-wise Sales Report";
                vm.ReportDescription = "Sales volumes, collections, and outstanding receivables per customer.";
                vm.Data = (await _salesReportService.GetCustomerWiseSalesAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "SalespersonWise":
                vm.ReportTitle = "Salesperson-wise Sales Report";
                vm.ReportDescription = "Staff billing performance, invoice volume, and average ticket size.";
                vm.Data = (await _salesReportService.GetSalespersonWiseSalesAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "SalesReturn":
                vm.ReportTitle = "Sales Return Report";
                vm.ReportDescription = "Customer returns, credit notes, and associated original invoice references.";
                vm.Data = (await _salesReportService.GetSalesReturnReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "Discount":
                vm.ReportTitle = "Discount Report";
                vm.ReportDescription = "Invoices with promotional discounts, concessions, and percentage impact.";
                vm.Data = (await _salesReportService.GetDiscountReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PaymentCollection":
                vm.ReportTitle = "Payment Collection Report";
                vm.ReportDescription = "Collections received against sales invoices and payment status tracking.";
                vm.Data = (await _salesReportService.GetPaymentCollectionReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "BillDetails":
                vm.ReportTitle = "Bill Details Report (Item Level)";
                vm.ReportDescription = "Item-by-item breakdown of products sold across all sales invoices.";
                vm.Data = (await _salesReportService.GetBillDetailsReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "CancelledSales":
                vm.ReportTitle = "Cancelled Sales Report";
                vm.ReportDescription = "Invoices marked as cancelled, cancellation user, and recorded reasons.";
                vm.Data = (await _salesReportService.GetCancelledSalesReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "DailySales":
            default:
                vm.ReportType = "DailySales";
                vm.ReportTitle = "Daily Sales Report";
                vm.ReportDescription = "Day-to-day sales invoices, gross turnover, taxes, collections, and receivables.";
                vm.Data = (await _salesReportService.GetDailySalesReportAsync(filter, ct)).Cast<object>().ToList();
                break;
        }

        vm.SummaryCards = await _salesReportService.GetSalesSummaryCardsAsync(filter, ct);
        vm.TotalRecords = vm.Data.Count;
        return View("Sales", vm);
    }

    // --- 3. Purchase Reports ---
    [HttpGet("Purchase")]
    [HttpGet("PurchaseReports")]
    public async Task<IActionResult> Purchase([FromQuery] ReportFilterViewModel filter, CancellationToken ct = default)
    {
        await PopulateFilterLookups();
        if (string.IsNullOrWhiteSpace(filter.ReportType)) filter.ReportType = "DailyPurchase";

        var vm = new ReportResultViewModel<object>
        {
            ReportType = filter.ReportType,
            Category = "Purchase",
            Filter = filter,
            ActiveCompanyName = ActiveCompanyName,
            ActiveCompanyCode = ActiveCompanyCode
        };

        switch (filter.ReportType)
        {
            case "MonthlyPurchase":
                vm.ReportTitle = "Monthly Purchase Report";
                vm.ReportDescription = "Monthly procurement volume, input tax credits, and cash outflow.";
                vm.Data = (await _purchaseReportService.GetMonthlyPurchaseReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PurchaseRegister":
                vm.ReportTitle = "Purchase Register";
                vm.ReportDescription = "Detailed register of all purchase invoices from vendors and suppliers.";
                vm.Data = (await _purchaseReportService.GetPurchaseRegisterAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "SupplierWise":
                vm.ReportTitle = "Supplier-wise Purchase Report";
                vm.ReportDescription = "Procurement volume, payments made, and outstanding payables per vendor.";
                vm.Data = (await _purchaseReportService.GetSupplierWisePurchaseAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "ProductWise":
                vm.ReportTitle = "Product-wise Purchase Report";
                vm.ReportDescription = "Quantity procured, total expenditure, and weighted average purchase cost.";
                vm.Data = (await _purchaseReportService.GetProductWisePurchaseAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PurchaseReturn":
                vm.ReportTitle = "Purchase Return Report";
                vm.ReportDescription = "Debit notes, goods returned to vendors, and credit balances.";
                vm.Data = (await _purchaseReportService.GetPurchaseReturnReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PurchaseOrder":
                vm.ReportTitle = "Purchase Order Report";
                vm.ReportDescription = "All purchase orders placed with suppliers and current status.";
                vm.Data = (await _purchaseReportService.GetPurchaseOrderReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PendingPO":
                vm.ReportTitle = "Pending Purchase Orders";
                vm.ReportDescription = "Orders awaiting fulfillment, goods receipt, or vendor invoicing.";
                vm.Data = (await _purchaseReportService.GetPendingPurchaseOrdersAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "GoodsReceipt":
                vm.ReportTitle = "Goods Receipt Report (GRN)";
                vm.ReportDescription = "Materials and inventory received into warehouses from suppliers.";
                vm.Data = (await _purchaseReportService.GetGoodsReceiptReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PurchasePayment":
                vm.ReportTitle = "Purchase Payment Report";
                vm.ReportDescription = "Payments disbursed to suppliers against purchase invoices.";
                vm.Data = (await _purchaseReportService.GetPurchasePaymentReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PriceHistory":
                vm.ReportTitle = "Purchase Price History";
                vm.ReportDescription = "Historical price trends and procurement rates across bills.";
                vm.Data = (await _purchaseReportService.GetPurchasePriceHistoryAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PurchaseTax":
                vm.ReportTitle = "Purchase Tax Summary (ITC)";
                vm.ReportDescription = "Input Tax Credit breakdown across GST slabs on procurement.";
                vm.Data = (await _purchaseReportService.GetPurchaseTaxSummaryAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "DailyPurchase":
            default:
                vm.ReportType = "DailyPurchase";
                vm.ReportTitle = "Daily Purchase Report";
                vm.ReportDescription = "Daily procurement turnover, taxes, and supplier invoice counts.";
                vm.Data = (await _purchaseReportService.GetDailyPurchaseReportAsync(filter, ct)).Cast<object>().ToList();
                break;
        }

        vm.SummaryCards = await _purchaseReportService.GetPurchaseSummaryCardsAsync(filter, ct);
        vm.TotalRecords = vm.Data.Count;
        return View("Purchase", vm);
    }

    // --- 4. Inventory Reports ---
    [HttpGet("Inventory")]
    [HttpGet("InventoryReports")]
    public async Task<IActionResult> Inventory([FromQuery] ReportFilterViewModel filter, CancellationToken ct = default)
    {
        await PopulateFilterLookups();
        if (string.IsNullOrWhiteSpace(filter.ReportType)) filter.ReportType = "CurrentStock";

        var vm = new ReportResultViewModel<object>
        {
            ReportType = filter.ReportType,
            Category = "Inventory",
            Filter = filter,
            ActiveCompanyName = ActiveCompanyName,
            ActiveCompanyCode = ActiveCompanyCode
        };

        switch (filter.ReportType)
        {
            case "LowStock":
                vm.ReportTitle = "Low Stock Report";
                vm.ReportDescription = "Products that have reached or fallen below configured minimum safety thresholds.";
                vm.Data = (await _inventoryReportService.GetLowStockReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "OutOfStock":
                vm.ReportTitle = "Out of Stock Report";
                vm.ReportDescription = "Critical alert: Products currently at zero or negative stock.";
                vm.Data = (await _inventoryReportService.GetOutOfStockReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "StockLedger":
                vm.ReportTitle = "Stock Ledger";
                vm.ReportDescription = "Historical chronological ledger statement of movements for a product.";
                int pid = filter.ProductId ?? 1;
                vm.Data = (await _inventoryReportService.GetStockLedgerAsync(pid, filter.WarehouseId, filter.FromDate ?? DateTime.Today.AddMonths(-1), filter.ToDate ?? DateTime.Today, ct)).Cast<object>().ToList();
                break;

            case "StockMovement":
                vm.ReportTitle = "Stock Movement Summary";
                vm.ReportDescription = "Reconstructed Opening + Inward - Outward = Closing stock movements.";
                vm.Data = (await _inventoryReportService.GetStockMovementReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "StockValuation":
                vm.ReportTitle = "Stock Valuation Report";
                vm.ReportDescription = "Inventory capital valuation computed at cost (Purchase Price) and retail (Sales Price).";
                vm.Data = (await _inventoryReportService.GetStockValuationReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "OpeningStock":
                vm.ReportTitle = "Opening Stock Report";
                vm.ReportDescription = "Configured opening stock quantities and valuations initialized in master.";
                vm.Data = (await _inventoryReportService.GetOpeningStockReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "ClosingStock":
                vm.ReportTitle = "Closing Stock (Historical Reconstructed)";
                vm.ReportDescription = "Reconstructed closing stock as of the selected cut-off date.";
                vm.Data = (await _inventoryReportService.GetClosingStockReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "StockTransfer":
                vm.ReportTitle = "Stock Transfer Report";
                vm.ReportDescription = "Inter-warehouse stock transfer dispatches and receipts.";
                vm.Data = (await _inventoryReportService.GetStockTransferReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "StockAdjustment":
                vm.ReportTitle = "Stock Adjustment Report";
                vm.ReportDescription = "Manual stock corrections, additions, deductions, and variance notes.";
                vm.Data = (await _inventoryReportService.GetStockAdjustmentReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PhysicalVerification":
                vm.ReportTitle = "Physical Stock Verification";
                vm.ReportDescription = "Audit count audits comparing physical floor count vs ERP book stock.";
                vm.Data = (await _inventoryReportService.GetPhysicalStockVerificationReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "FastMoving":
                vm.ReportTitle = "Fast Moving Products";
                vm.ReportDescription = "High turnover goods sorted by sales frequency and sales volume.";
                vm.Data = (await _inventoryReportService.GetFastMovingProductsAsync(filter, 20, ct)).Cast<object>().ToList();
                break;

            case "SlowMoving":
                vm.ReportTitle = "Slow Moving Products";
                vm.ReportDescription = "Products with low turnover rate in the selected date range.";
                vm.Data = (await _inventoryReportService.GetSlowMovingProductsAsync(filter, 20, ct)).Cast<object>().ToList();
                break;

            case "NonMoving":
                vm.ReportTitle = "Non-Moving Products (Dead Stock)";
                vm.ReportDescription = "Items with active inventory but zero sales in the last 90 days.";
                vm.Data = (await _inventoryReportService.GetNonMovingProductsAsync(filter, 90, ct)).Cast<object>().ToList();
                break;

            case "ReorderSuggestions":
                vm.ReportTitle = "Intelligent Reorder Suggestions";
                vm.ReportDescription = "Recommended purchase orders based on minimum stock and reorder levels.";
                vm.Data = (await _inventoryReportService.GetReorderSuggestionsAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "CurrentStock":
            default:
                vm.ReportType = "CurrentStock";
                vm.ReportTitle = "Current Stock Report";
                vm.ReportDescription = "Live on-hand inventory levels, warehouse locations, and stock valuation.";
                vm.Data = (await _inventoryReportService.GetCurrentStockReportAsync(filter, ct)).Cast<object>().ToList();
                break;
        }

        vm.SummaryCards = await _inventoryReportService.GetInventorySummaryCardsAsync(filter, ct);
        vm.TotalRecords = vm.Data.Count;
        return View("Inventory", vm);
    }

    // --- 5. Accounts Reports ---
    [HttpGet("Accounts")]
    [HttpGet("AccountingReports")]
    public async Task<IActionResult> Accounts([FromQuery] ReportFilterViewModel filter, CancellationToken ct = default)
    {
        await PopulateFilterLookups();
        if (string.IsNullOrWhiteSpace(filter.ReportType)) filter.ReportType = "CustomerOutstanding";

        var vm = new ReportResultViewModel<object>
        {
            ReportType = filter.ReportType,
            Category = "Accounts",
            Filter = filter,
            ActiveCompanyName = ActiveCompanyName,
            ActiveCompanyCode = ActiveCompanyCode
        };

        var (from, to) = ReportFilterHelper.ResolveDateRange(filter);

        switch (filter.ReportType)
        {
            case "SupplierOutstanding":
                vm.ReportTitle = "Supplier Outstanding (Accounts Payable)";
                vm.ReportDescription = "Outstanding bills due to vendors, overdue tracking, and credit terms.";
                vm.Data = (await _accountsReportService.GetSupplierOutstandingAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "CustomerLedger":
                vm.ReportTitle = "Customer Ledger Statement";
                vm.ReportDescription = "Chronological statement of invoices, receipts, and running balance.";
                int cId = filter.CustomerId ?? 1;
                vm.Data = (await _accountsReportService.GetCustomerLedgerStatementAsync(cId, from, to, ct)).Cast<object>().ToList();
                break;

            case "SupplierLedger":
                vm.ReportTitle = "Supplier Ledger Statement";
                vm.ReportDescription = "Chronological statement of bills, payments, and running balance.";
                int sId = filter.SupplierId ?? 1;
                vm.Data = (await _accountsReportService.GetSupplierLedgerStatementAsync(sId, from, to, ct)).Cast<object>().ToList();
                break;

            case "CashBook":
                vm.ReportTitle = "Cash Book";
                vm.ReportDescription = "Cash receipts, payments, and running cash-in-hand balance.";
                vm.Data = (await _accountsReportService.GetCashBookAsync(from, to, ct)).Cast<object>().ToList();
                break;

            case "BankBook":
                vm.ReportTitle = "Bank Book";
                vm.ReportDescription = "Bank account transactions, deposits, withdrawals, and bank balance.";
                vm.Data = (await _accountsReportService.GetBankBookAsync(from, to, null, ct)).Cast<object>().ToList();
                break;

            case "ReceiptVouchers":
                vm.ReportTitle = "Receipt Voucher Register";
                vm.ReportDescription = "Official receipts recorded in double-entry bookkeeping.";
                vm.Data = (await _accountsReportService.GetReceiptVouchersAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PaymentVouchers":
                vm.ReportTitle = "Payment Voucher Register";
                vm.ReportDescription = "Official payments disbursed and recorded in accounts.";
                vm.Data = (await _accountsReportService.GetPaymentVouchersAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "JournalVouchers":
                vm.ReportTitle = "Journal Voucher Register";
                vm.ReportDescription = "Adjustment, depreciation, and transfer journal vouchers.";
                vm.Data = (await _accountsReportService.GetJournalVouchersAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "LedgerSummary":
                vm.ReportTitle = "General Ledger Summary";
                vm.ReportDescription = "Summary of opening balance, debit/credit turnovers, and closing balance.";
                vm.Data = (await _accountsReportService.GetLedgerSummaryAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "TrialBalance":
                vm.ReportTitle = "Trial Balance";
                vm.ReportDescription = "Trial balance verifying total debits equal total credits across all accounts.";
                vm.Data = (await _accountsReportService.GetTrialBalanceAsync(to, ct)).Cast<object>().ToList();
                break;

            case "ProfitAndLoss":
                vm.ReportTitle = "Profit & Loss Statement";
                vm.ReportDescription = "Trading and operational income, COGS, expenses, and net profit.";
                var pnl = await _accountsReportService.GetProfitAndLossAsync(from, to, ct);
                ViewBag.PnlData = pnl;
                vm.Data = new List<object> { pnl };
                break;

            case "CustomerOutstanding":
            default:
                vm.ReportType = "CustomerOutstanding";
                vm.ReportTitle = "Customer Outstanding (Accounts Receivable)";
                vm.ReportDescription = "Pending balances receivable from customers, credit limits, and aging status.";
                vm.Data = (await _accountsReportService.GetCustomerOutstandingAsync(filter, ct)).Cast<object>().ToList();
                break;
        }

        vm.SummaryCards = await _accountsReportService.GetAccountsSummaryCardsAsync(filter, ct);
        vm.TotalRecords = vm.Data.Count;
        return View("Accounts", vm);
    }

    // --- 6. GST Reports ---
    [HttpGet("GST")]
    public async Task<IActionResult> GST([FromQuery] ReportFilterViewModel filter, CancellationToken ct = default)
    {
        await PopulateFilterLookups();
        if (string.IsNullOrWhiteSpace(filter.ReportType)) filter.ReportType = "GstSalesSummary";

        var vm = new ReportResultViewModel<object>
        {
            ReportType = filter.ReportType,
            Category = "GST",
            Filter = filter,
            ActiveCompanyName = ActiveCompanyName,
            ActiveCompanyCode = ActiveCompanyCode
        };

        switch (filter.ReportType)
        {
            case "GstPurchaseSummary":
                vm.ReportTitle = "GST Purchase Summary (GSTR-2/3B ITC Preparation)";
                vm.ReportDescription = "Inward supplies and Eligible Input Tax Credit (ITC) data.";
                vm.Data = (await _gstReportService.GetGstPurchaseSummaryAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "HsnSales":
                vm.ReportTitle = "HSN-wise Sales Summary (GSTR-1)";
                vm.ReportDescription = "HSN Code summary of taxable outward supplies and tax rates.";
                vm.Data = (await _gstReportService.GetHsnWiseSalesAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "HsnPurchase":
                vm.ReportTitle = "HSN-wise Purchase Summary";
                vm.ReportDescription = "HSN Code breakdown of inward supplies and input taxes.";
                vm.Data = (await _gstReportService.GetHsnWisePurchaseAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "CgstSummary":
                vm.ReportTitle = "CGST Summary";
                vm.ReportDescription = "Central Goods and Services Tax collected across rates.";
                vm.Data = (await _gstReportService.GetCgstSummaryAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "SgstSummary":
                vm.ReportTitle = "SGST Summary";
                vm.ReportDescription = "State Goods and Services Tax collected across rates.";
                vm.Data = (await _gstReportService.GetSgstSummaryAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "IgstSummary":
                vm.ReportTitle = "IGST Summary";
                vm.ReportDescription = "Integrated Goods and Services Tax on interstate transactions.";
                vm.Data = (await _gstReportService.GetIgstSummaryAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "TaxRateSummary":
                vm.ReportTitle = "Tax Rate-wise Summary";
                vm.ReportDescription = "Taxable value and tax breakdown by slab (0%, 5%, 12%, 18%, 28%).";
                vm.Data = (await _gstReportService.GetTaxRateWiseSummaryAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "SalesReturnTax":
                vm.ReportTitle = "Sales Return Tax Adjustment";
                vm.ReportDescription = "Tax reversals on customer returns and credit notes.";
                vm.Data = (await _gstReportService.GetSalesReturnTaxSummaryAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PurchaseReturnTax":
                vm.ReportTitle = "Purchase Return Tax Adjustment";
                vm.ReportDescription = "Input tax credit reversals on vendor returns.";
                vm.Data = (await _gstReportService.GetPurchaseReturnTaxSummaryAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "GstSalesSummary":
            default:
                vm.ReportType = "GstSalesSummary";
                vm.ReportTitle = "GST Sales Summary (GSTR-1 Preparation)";
                vm.ReportDescription = "Outward supplies categorized by B2B registered and B2C consumer transactions.";
                vm.Data = (await _gstReportService.GetGstSalesSummaryAsync(filter, ct)).Cast<object>().ToList();
                break;
        }

        vm.SummaryCards = await _gstReportService.GetGstSummaryCardsAsync(filter, ct);
        vm.TotalRecords = vm.Data.Count;
        return View("GST", vm);
    }

    // --- 7. Customer Reports ---
    [HttpGet("Customers")]
    public async Task<IActionResult> Customers([FromQuery] ReportFilterViewModel filter, CancellationToken ct = default)
    {
        await PopulateFilterLookups();
        if (string.IsNullOrWhiteSpace(filter.ReportType)) filter.ReportType = "CustomerList";

        var vm = new ReportResultViewModel<object>
        {
            ReportType = filter.ReportType,
            Category = "Customers",
            Filter = filter,
            ActiveCompanyName = ActiveCompanyName,
            ActiveCompanyCode = ActiveCompanyCode
        };

        var (from, to) = ReportFilterHelper.ResolveDateRange(filter);

        switch (filter.ReportType)
        {
            case "NewCustomers":
                vm.ReportTitle = "New Customers Report";
                vm.ReportDescription = "Customers onboarded within the selected date range.";
                vm.Data = (await _custSuppReportService.GetNewCustomersAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "TopCustomers":
                vm.ReportTitle = "Top Customers (By Sales)";
                vm.ReportDescription = "Highest revenue generating customer accounts.";
                vm.Data = (await _custSuppReportService.GetTopCustomersAsync(filter, 20, ct)).Cast<object>().ToList();
                break;

            case "PurchaseHistory":
                vm.ReportTitle = "Customer Purchase History";
                vm.ReportDescription = "History of invoices and items purchased by customer.";
                int cId = filter.CustomerId ?? 1;
                vm.Data = (await _custSuppReportService.GetCustomerPurchaseHistoryAsync(cId, filter, ct)).Cast<object>().ToList();
                break;

            case "Outstanding":
                vm.ReportTitle = "Customer Outstanding Receivables";
                vm.ReportDescription = "Unpaid customer invoice balances and overdue invoices.";
                vm.Data = (await _custSuppReportService.GetCustomerOutstandingAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "InactiveCustomers":
                vm.ReportTitle = "Inactive Customers";
                vm.ReportDescription = "Customers with no orders in the past 60 days.";
                vm.Data = (await _custSuppReportService.GetInactiveCustomersAsync(filter, 60, ct)).Cast<object>().ToList();
                break;

            case "CustomerList":
            default:
                vm.ReportType = "CustomerList";
                vm.ReportTitle = "Customer Master List";
                vm.ReportDescription = "Complete directory of customers with contact, GSTIN, and current balance.";
                vm.Data = (await _custSuppReportService.GetCustomerListAsync(filter, ct)).Cast<object>().ToList();
                break;
        }

        vm.SummaryCards = await _custSuppReportService.GetCustomerSummaryCardsAsync(filter, ct);
        vm.TotalRecords = vm.Data.Count;
        return View("Customers", vm);
    }

    // --- 8. Supplier Reports ---
    [HttpGet("Suppliers")]
    public async Task<IActionResult> Suppliers([FromQuery] ReportFilterViewModel filter, CancellationToken ct = default)
    {
        await PopulateFilterLookups();
        if (string.IsNullOrWhiteSpace(filter.ReportType)) filter.ReportType = "SupplierList";

        var vm = new ReportResultViewModel<object>
        {
            ReportType = filter.ReportType,
            Category = "Suppliers",
            Filter = filter,
            ActiveCompanyName = ActiveCompanyName,
            ActiveCompanyCode = ActiveCompanyCode
        };

        switch (filter.ReportType)
        {
            case "TopSuppliers":
                vm.ReportTitle = "Top Suppliers (By Procurement)";
                vm.ReportDescription = "Vendors with highest procurement volume and spend.";
                vm.Data = (await _custSuppReportService.GetTopSuppliersAsync(filter, 20, ct)).Cast<object>().ToList();
                break;

            case "PurchaseHistory":
                vm.ReportTitle = "Supplier Purchase History";
                vm.ReportDescription = "Invoices and products received from the selected supplier.";
                int sId = filter.SupplierId ?? 1;
                vm.Data = (await _custSuppReportService.GetSupplierPurchaseHistoryAsync(sId, filter, ct)).Cast<object>().ToList();
                break;

            case "Outstanding":
                vm.ReportTitle = "Supplier Outstanding Payables";
                vm.ReportDescription = "Pending balances due to vendors and payment terms.";
                vm.Data = (await _custSuppReportService.GetSupplierOutstandingAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PaymentHistory":
                vm.ReportTitle = "Supplier Payment History";
                vm.ReportDescription = "Disbursements and payment records for vendor bills.";
                int suppId = filter.SupplierId ?? 1;
                vm.Data = (await _custSuppReportService.GetSupplierPaymentHistoryAsync(suppId, filter, ct)).Cast<object>().ToList();
                break;

            case "SupplierList":
            default:
                vm.ReportType = "SupplierList";
                vm.ReportTitle = "Supplier Master List";
                vm.ReportDescription = "Directory of approved vendors with contact and balance details.";
                vm.Data = (await _custSuppReportService.GetSupplierListAsync(filter, ct)).Cast<object>().ToList();
                break;
        }

        vm.SummaryCards = await _custSuppReportService.GetSupplierSummaryCardsAsync(filter, ct);
        vm.TotalRecords = vm.Data.Count;
        return View("Suppliers", vm);
    }

    // --- 9. Employee & Audit Reports ---
    [HttpGet("Audit")]
    public async Task<IActionResult> Audit([FromQuery] ReportFilterViewModel filter, CancellationToken ct = default)
    {
        await PopulateFilterLookups();
        if (string.IsNullOrWhiteSpace(filter.ReportType)) filter.ReportType = "AuditTrail";

        var vm = new ReportResultViewModel<object>
        {
            ReportType = filter.ReportType,
            Category = "Audit",
            Filter = filter,
            ActiveCompanyName = ActiveCompanyName,
            ActiveCompanyCode = ActiveCompanyCode
        };

        switch (filter.ReportType)
        {
            case "EmployeeSales":
                vm.ReportTitle = "Employee-wise Sales Performance";
                vm.ReportDescription = "Sales turnover, billing count, and performance by staff member.";
                vm.Data = (await _auditReportService.GetEmployeeWiseSalesAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "LoginHistory":
                vm.ReportTitle = "User Login History";
                vm.ReportDescription = "System authentication log with IP addresses, devices, and session status.";
                vm.Data = (await _auditReportService.GetLoginHistoryReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "UserActivity":
                vm.ReportTitle = "User Activity Report";
                vm.ReportDescription = "User actions, context switching, and critical operational events.";
                vm.Data = (await _auditReportService.GetUserActivityReportAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "BillModifications":
                vm.ReportTitle = "Bill Modification History";
                vm.ReportDescription = "Audit trail of updates and alterations made to invoices and bills.";
                vm.Data = (await _auditReportService.GetBillModificationHistoryAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "DeletedTransactions":
                vm.ReportTitle = "Deleted Transaction History";
                vm.ReportDescription = "Historical records of transactions that were voided or removed.";
                vm.Data = (await _auditReportService.GetDeletedTransactionHistoryAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "CashierSales":
                vm.ReportTitle = "Cashier-wise Sales Attribution";
                vm.ReportDescription = "Sales billing turnover attributed by checkout operator / cashier.";
                vm.Data = (await _auditReportService.GetCashierWiseSalesAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "AuditTrail":
            default:
                vm.ReportType = "AuditTrail";
                vm.ReportTitle = "Complete Audit Trail";
                vm.ReportDescription = "Chronological system audit log across all ERP modules.";
                vm.Data = (await _auditReportService.GetAuditTrailReportAsync(filter, ct)).Cast<object>().ToList();
                break;
        }

        vm.SummaryCards = await _auditReportService.GetAuditSummaryCardsAsync(filter, ct);
        vm.TotalRecords = vm.Data.Count;
        return View("Audit", vm);
    }

    // --- 10. Business Intelligence ---
    [HttpGet("BI")]
    public async Task<IActionResult> BI([FromQuery] ReportFilterViewModel filter, CancellationToken ct = default)
    {
        await PopulateFilterLookups();
        if (string.IsNullOrWhiteSpace(filter.ReportType)) filter.ReportType = "SalesGrowth";

        var vm = new ReportResultViewModel<object>
        {
            ReportType = filter.ReportType,
            Category = "BI",
            Filter = filter,
            ActiveCompanyName = ActiveCompanyName,
            ActiveCompanyCode = ActiveCompanyCode
        };

        switch (filter.ReportType)
        {
            case "MonthOverMonth":
                vm.ReportTitle = "Month-over-Month Comparison";
                vm.ReportDescription = "Performance variances between current and preceding month.";
                vm.Data = (await _biService.GetMonthOverMonthComparisonAsync(ct)).Cast<object>().ToList();
                break;

            case "YearOverYear":
                vm.ReportTitle = "Year-over-Year Comparison";
                vm.ReportDescription = "Annual trade growth and financial trajectory comparisons.";
                vm.Data = (await _biService.GetYearOverYearComparisonAsync(ct)).Cast<object>().ToList();
                break;

            case "Top10Products":
                vm.ReportTitle = "Top 10 Products by Revenue";
                vm.ReportDescription = "Best performing catalogue items and estimated margin contributions.";
                vm.Data = (await _biService.GetTop10ProductsAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "Bottom10Products":
                vm.ReportTitle = "Bottom 10 Products";
                vm.ReportDescription = "Lowest revenue products requiring pricing or marketing review.";
                vm.Data = (await _biService.GetBottom10ProductsAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "CategoryPerformance":
                vm.ReportTitle = "Category Performance Analytics";
                vm.ReportDescription = "Category revenue share and volume distribution.";
                vm.Data = (await _biService.GetCategoryPerformanceAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "SalesTrends":
                vm.ReportTitle = "Product Sales Trends";
                vm.ReportDescription = "Day-to-day sales movement trajectories.";
                vm.Data = (await _biService.GetProductSalesTrendsAsync(filter.ProductId, filter, ct)).Cast<object>().ToList();
                break;

            case "DeadStock":
                vm.ReportTitle = "Dead Stock Capital Analysis";
                vm.ReportDescription = "Capital locked up in non-moving inventory (>90 days).";
                vm.Data = (await _biService.GetDeadStockAnalysisAsync(90, ct)).Cast<object>().ToList();
                break;

            case "InventoryTurnover":
                vm.ReportTitle = "Inventory Turnover & Days Sales of Inventory";
                vm.ReportDescription = "Cost of Goods Sold vs average stock ratio and restocking velocity.";
                vm.Data = (await _biService.GetInventoryTurnoverAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "ReorderSuggestions":
                vm.ReportTitle = "Reorder Restocking Suggestions";
                vm.ReportDescription = "Procurement recommendations to prevent stockouts.";
                vm.Data = (await _biService.GetReorderSuggestionsBiAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "PurchaseVsSales":
                vm.ReportTitle = "Purchase vs Sales Margin Analysis";
                vm.ReportDescription = "Revenue vs procurement cost comparison and gross margin tracking.";
                vm.Data = (await _biService.GetPurchaseVsSalesAnalysisAsync(filter, ct)).Cast<object>().ToList();
                break;

            case "SalesGrowth":
            default:
                vm.ReportType = "SalesGrowth";
                vm.ReportTitle = "Sales Growth Analysis";
                vm.ReportDescription = "Revenue expansion trajectory and growth rates over time.";
                vm.Data = (await _biService.GetSalesGrowthAnalysisAsync(filter, ct)).Cast<object>().ToList();
                break;
        }

        vm.SummaryCards = await _biService.GetBiSummaryCardsAsync(filter, ct);
        vm.TotalRecords = vm.Data.Count;
        return View("BI", vm);
    }

    // --- Export Endpoints ---
    [HttpGet("ExportExcel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] string category,
        [FromQuery] string reportType,
        [FromQuery] ReportFilterViewModel filter,
        CancellationToken ct = default)
    {
        filter.Category = category;
        filter.ReportType = reportType;

        var (data, headers, title) = await ResolveReportDataAndHeaders(category, reportType, filter, ct);
        var bytes = _exportService.ExportToExcel(title, headers, data, filter, ActiveCompanyName, "", null);

        var filename = $"{category}_{reportType}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }

    [HttpGet("ExportPdf")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] string category,
        [FromQuery] string reportType,
        [FromQuery] ReportFilterViewModel filter,
        CancellationToken ct = default)
    {
        filter.Category = category;
        filter.ReportType = reportType;

        var (data, headers, title) = await ResolveReportDataAndHeaders(category, reportType, filter, ct);
        var bytes = _exportService.ExportToPdf(title, headers, data, filter, ActiveCompanyName, "", null);

        var filename = $"{category}_{reportType}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
        return File(bytes, "application/pdf", filename);
    }

    private async Task<(List<object> Data, List<string> Headers, string Title)> ResolveReportDataAndHeaders(
        string category,
        string reportType,
        ReportFilterViewModel filter,
        CancellationToken ct)
    {
        switch (category.ToLowerInvariant())
        {
            case "sales":
                if (reportType == "SalesRegister")
                {
                    var items = await _salesReportService.GetSalesRegisterAsync(filter, ct);
                    return (items.Cast<object>().ToList(), new List<string> { "Invoice #", "Date", "Customer", "GSTIN", "Salesperson", "SubTotal", "Discount", "Tax", "Grand Total", "Paid", "Balance", "Status" }, "Sales Register");
                }
                else if (reportType == "ProductWise")
                {
                    var items = await _salesReportService.GetProductWiseSalesAsync(filter, ct);
                    return (items.Cast<object>().ToList(), new List<string> { "Code", "Product Name", "Category", "Brand", "Unit", "Qty Sold", "Rate", "Tax", "Total Sales", "% of Sales" }, "Product-wise Sales");
                }
                else
                {
                    var items = await _salesReportService.GetDailySalesReportAsync(filter, ct);
                    return (items.Cast<object>().ToList(), new List<string> { "Date", "Invoices", "Gross Sales", "Discounts", "Tax Amount", "Net Sales", "Collected", "Balance" }, "Daily Sales Report");
                }

            case "purchase":
                if (reportType == "PurchaseRegister")
                {
                    var items = await _purchaseReportService.GetPurchaseRegisterAsync(filter, ct);
                    return (items.Cast<object>().ToList(), new List<string> { "Bill #", "Date", "Supplier", "GSTIN", "Ref #", "SubTotal", "Discount", "Tax", "Grand Total", "Paid", "Balance", "Status" }, "Purchase Register");
                }
                else
                {
                    var items = await _purchaseReportService.GetDailyPurchaseReportAsync(filter, ct);
                    return (items.Cast<object>().ToList(), new List<string> { "Date", "Bills", "Gross Purchases", "Discounts", "Tax Amount", "Net Purchases", "Paid", "Balance" }, "Daily Purchase Report");
                }

            case "inventory":
                if (reportType == "LowStock")
                {
                    var items = await _inventoryReportService.GetLowStockReportAsync(filter, ct);
                    return (items.Cast<object>().ToList(), new List<string> { "Code", "Product Name", "Category", "Brand", "Warehouse", "Unit", "Current Stock", "Min Stock", "Reorder Level", "Cost Price", "Selling Price" }, "Low Stock Report");
                }
                else
                {
                    var items = await _inventoryReportService.GetCurrentStockReportAsync(filter, ct);
                    return (items.Cast<object>().ToList(), new List<string> { "Code", "Product Name", "Category", "Brand", "Warehouse", "Unit", "Current Stock", "Min Stock", "Reorder Level", "Cost Price", "Selling Price" }, "Current Stock Report");
                }

            case "accounts":
                var acc = await _accountsReportService.GetCustomerOutstandingAsync(filter, ct);
                return (acc.Cast<object>().ToList(), new List<string> { "Code", "Customer Name", "Phone", "City", "Opening Bal", "Type", "Invoiced", "Paid", "Net Outstanding", "Overdue Bills", "Credit Days", "Credit Limit" }, "Customer Outstanding");

            case "gst":
                var gst = await _gstReportService.GetTaxRateWiseSummaryAsync(filter, ct);
                return (gst.Cast<object>().ToList(), new List<string> { "Tax Rate %", "Taxable Value", "CGST", "SGST", "IGST" }, "Tax Rate-wise Summary");

            default:
                var def = await _salesReportService.GetDailySalesReportAsync(filter, ct);
                return (def.Cast<object>().ToList(), new List<string> { "Date", "Invoices", "Gross Sales", "Discounts", "Tax Amount", "Net Sales", "Collected", "Balance" }, "Sales Report");
        }
    }
}
