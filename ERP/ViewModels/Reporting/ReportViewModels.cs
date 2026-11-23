using System;
using System.Collections.Generic;

namespace ERP.ViewModels.Reporting;

public class ReportFilterViewModel
{
    public string ReportType { get; set; } = "DailySales";
    public string Category { get; set; } = "Sales";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? DatePreset { get; set; } = "this_month"; // today, yesterday, this_week, this_month, last_month, this_year, custom
    public string? FinancialYear { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? WarehouseId { get; set; }
    public string? Status { get; set; }
    public string? Salesperson { get; set; }
    public string? VoucherType { get; set; }
    public string? SearchTerm { get; set; }
    public string Mode { get; set; } = "Advanced"; // Simple, Advanced
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; } = "asc";
}

public class ReportSummaryCard
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Icon { get; set; } = "fa-chart-pie";
    public string ColorClass { get; set; } = "primary"; // primary, success, info, warning, danger
}

public class ReportResultViewModel<T>
{
    public string ReportTitle { get; set; } = string.Empty;
    public string ReportDescription { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public ReportFilterViewModel Filter { get; set; } = new();
    public List<ReportSummaryCard> SummaryCards { get; set; } = new();
    public List<T> Data { get; set; } = new();
    public int TotalRecords { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 1;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string ActiveCompanyName { get; set; } = string.Empty;
    public string ActiveCompanyCode { get; set; } = string.Empty;
    public string? CompanyLogo { get; set; }
    public string Currency { get; set; } = "₹";
    public bool IsSimpleMode => Filter.Mode == "Simple";
    public Dictionary<string, string> FilterLookups { get; set; } = new();
}

#region Sales Report DTOs

public class DailySalesDto
{
    public DateTime Date { get; set; }
    public string DateDisplay => Date.ToString("dd-MMM-yyyy");
    public int InvoiceCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetSales { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
}

public class MonthlySalesDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetSales { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal GrowthRate { get; set; }
}

public class YearlySalesDto
{
    public int Year { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetSales { get; set; }
    public decimal TotalReturns { get; set; }
}

public class SalesRegisterDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? GSTNumber { get; set; }
    public string? Salesperson { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool WithGST { get; set; }
}

public class ProductWiseSalesDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal TotalSalesAmount { get; set; }
    public decimal AverageRate { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

public class CategoryWiseSalesDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal QuantitySold { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

public class BrandWiseSalesDto
{
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

public class CustomerWiseSalesDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string? City { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
}

public class SalespersonWiseSalesDto
{
    public string Salesperson { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal AverageTicketSize => InvoiceCount > 0 ? Math.Round(TotalSalesAmount / InvoiceCount, 2) : 0;
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
}

public class SalesReturnReportDto
{
    public int ReturnId { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? OriginalInvoiceNumber { get; set; }
    public string? Reason { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DiscountReportDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercentage => SubTotal > 0 ? Math.Round((DiscountAmount / SubTotal) * 100, 2) : 0;
    public decimal GrandTotal { get; set; }
}

public class PaymentCollectionReportDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string PaymentStatus => BalanceAmount <= 0 ? "Fully Paid" : PaidAmount > 0 ? "Partially Paid" : "Unpaid";
    public string? PaymentTerms { get; set; }
}

public class BillDetailsReportDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? HSNCode { get; set; }
    public decimal Quantity { get; set; }
    public string? UnitName { get; set; }
    public decimal Rate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ItemAmount { get; set; }
}

public class CancelledSalesReportDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CancelledDate { get; set; }
}

#endregion

#region Purchase Report DTOs

public class DailyPurchaseDto
{
    public DateTime Date { get; set; }
    public string DateDisplay => Date.ToString("dd-MMM-yyyy");
    public int BillCount { get; set; }
    public decimal GrossPurchase { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetPurchase { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
}

public class MonthlyPurchaseDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int BillCount { get; set; }
    public decimal GrossPurchase { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetPurchase { get; set; }
    public decimal PaidAmount { get; set; }
}

public class PurchaseRegisterDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? GSTNumber { get; set; }
    public string? ReferenceNumber { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool WithGST { get; set; }
}

public class SupplierWisePurchaseDto
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? City { get; set; }
    public int BillCount { get; set; }
    public decimal TotalPurchaseAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
}

public class ProductWisePurchaseDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal QuantityPurchased { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal TotalPurchaseAmount { get; set; }
    public decimal AverageRate { get; set; }
    public decimal TotalTaxAmount { get; set; }
}

public class PurchaseReturnReportDto
{
    public int ReturnId { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? OriginalInvoiceNumber { get; set; }
    public string? Reason { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PurchaseOrderReportDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
}

public class GoodsReceiptReportDto
{
    public int GrnId { get; set; }
    public string GrnNumber { get; set; } = string.Empty;
    public DateTime GrnDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? PurchaseOrderNumber { get; set; }
    public string? ChallanNumber { get; set; }
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PurchasePaymentReportDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string PaymentStatus => BalanceAmount <= 0 ? "Fully Paid" : PaidAmount > 0 ? "Partially Paid" : "Unpaid";
}

public class PurchasePriceHistoryDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal PurchaseRate { get; set; }
}

public class PurchaseTaxSummaryDto
{
    public decimal TaxPercentage { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TotalTaxAmount => CGSTAmount + SGSTAmount + IGSTAmount;
}

#endregion

#region Inventory Report DTOs

public class CurrentStockDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalesPrice { get; set; }
    public decimal StockValuation => CurrentStock * PurchasePrice;
    public string StockStatus => CurrentStock <= 0 ? "Out of Stock" : CurrentStock <= MinimumStock ? "Low Stock" : "Normal";
}

public class StockLedgerRowDto
{
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal BalanceQuantity { get; set; }
    public decimal Rate { get; set; }
    public string? Remarks { get; set; }
}

public class StockMovementDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal OpeningStock { get; set; }
    public decimal InwardQuantity { get; set; }
    public decimal OutwardQuantity { get; set; }
    public decimal ClosingStock => OpeningStock + InwardQuantity - OutwardQuantity;
}

public class StockTransferReportDto
{
    public int TransferId { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public string FromWarehouse { get; set; } = string.Empty;
    public string ToWarehouse { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class StockAdjustmentReportDto
{
    public int AdjustmentId { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string AdjustmentType { get; set; } = string.Empty; // Addition, Deduction
    public int ItemCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public string? Remarks { get; set; }
}

public class PhysicalVerificationReportDto
{
    public int VerificationId { get; set; }
    public string VerificationNumber { get; set; } = string.Empty;
    public DateTime VerificationDate { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalVariance { get; set; }
    public string? Remarks { get; set; }
}

public class ProductMovementAnalysisDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal QuantitySold { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public int TransactionCount { get; set; }
    public DateTime? LastSaleDate { get; set; }
    public string MovementClassification { get; set; } = "Normal"; // Fast Moving, Slow Moving, Non-Moving
}

public class ReorderSuggestionDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal SuggestedOrderQty { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal EstimatedOrderCost => SuggestedOrderQty * PurchasePrice;
}

#endregion

#region Accounts Report DTOs

public class OutstandingReportDto
{
    public int EntityId { get; set; }
    public string EntityCode { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? City { get; set; }
    public decimal OpeningBalance { get; set; }
    public string BalanceType { get; set; } = "Dr";
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal NetOutstanding { get; set; }
    public int OverdueBillsCount { get; set; }
    public int? CreditDays { get; set; }
    public decimal CreditLimit { get; set; }
}

public class LedgerStatementRowDto
{
    public DateTime Date { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string VoucherType { get; set; } = string.Empty;
    public string Particulars { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
    public string BalanceType { get; set; } = "Dr";
}

public class CashBankBookRowDto
{
    public DateTime Date { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public string VoucherType { get; set; } = string.Empty;
    public string LedgerName { get; set; } = string.Empty;
    public string? Narration { get; set; }
    public decimal Receipts { get; set; }
    public decimal Payments { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class VoucherReportDto
{
    public int VoucherId { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; }
    public string VoucherType { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Narration { get; set; }
    public decimal TotalAmount { get; set; }
    public string AccountSummary { get; set; } = string.Empty;
}

public class LedgerSummaryDto
{
    public int LedgerId { get; set; }
    public string LedgerCode { get; set; } = string.Empty;
    public string LedgerName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public string OpeningType { get; set; } = "Dr";
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
    public string ClosingType { get; set; } = "Dr";
}

public class TrialBalanceRowDto
{
    public int LedgerId { get; set; }
    public string LedgerName { get; set; } = string.Empty;
    public string AccountGroup { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
}

public class ProfitAndLossReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal SalesRevenue { get; set; }
    public decimal SalesReturns { get; set; }
    public decimal NetSales => SalesRevenue - SalesReturns;
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit => NetSales - CostOfGoodsSold;
    public decimal GrossMarginPercentage => NetSales > 0 ? Math.Round((GrossProfit / NetSales) * 100, 2) : 0;
    public decimal IndirectIncomes { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal NetProfit => GrossProfit + IndirectIncomes - OperatingExpenses;
    public List<LedgerSummaryDto> IncomeLedgers { get; set; } = new();
    public List<LedgerSummaryDto> ExpenseLedgers { get; set; } = new();
}

#endregion

#region GST Report DTOs

public class GstSalesSummaryDto
{
    public string SupplyType { get; set; } = "B2B"; // B2B, B2C Large, B2C Small
    public int InvoiceCount { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TotalTax => CGSTAmount + SGSTAmount + IGSTAmount;
    public decimal InvoiceTotal => TaxableValue + TotalTax;
}

public class GstPurchaseSummaryDto
{
    public string SupplyType { get; set; } = "Inward Supplies";
    public int BillCount { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal EligibleCGST { get; set; }
    public decimal EligibleSGST { get; set; }
    public decimal EligibleIGST { get; set; }
    public decimal TotalITC => EligibleCGST + EligibleSGST + EligibleIGST;
}

public class HsnSummaryDto
{
    public string HSNCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal RateOfTax { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TotalTax => CGSTAmount + SGSTAmount + IGSTAmount;
}

public class TaxRateSummaryDto
{
    public decimal TaxRate { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TotalTax => CGSTAmount + SGSTAmount + IGSTAmount;
}

public class TaxBreakdownDto
{
    public string TaxType { get; set; } = string.Empty; // CGST, SGST, IGST
    public decimal TaxRate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
}

#endregion

#region Customer & Supplier Report DTOs

public class CustomerMasterReportDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? GSTNumber { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class SupplierMasterReportDto
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? GSTNumber { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class InactiveEntityDto
{
    public int EntityId { get; set; }
    public string EntityCode { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public int DaysSinceLastTransaction => LastTransactionDate.HasValue ? (int)(DateTime.Today - LastTransactionDate.Value.Date).TotalDays : 999;
    public decimal LifetimeSalesAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
}

#endregion

#region Audit & Employee Report DTOs

public class EmployeeSalesReportDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal AverageInvoiceValue => InvoiceCount > 0 ? Math.Round(TotalSalesAmount / InvoiceCount, 2) : 0;
}

public class LoginHistoryReportDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Role { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime? LogoutTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? IPAddress { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Device { get; set; }
}

public class UserActivityReportDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IPAddress { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditTrailReportDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

#endregion

#region Business Intelligence DTOs

public class GrowthAnalysisDto
{
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal SalesAmount { get; set; }
    public decimal PreviousPeriodSales { get; set; }
    public decimal GrowthAmount => SalesAmount - PreviousPeriodSales;
    public decimal GrowthPercentage => PreviousPeriodSales > 0 ? Math.Round((GrowthAmount / PreviousPeriodSales) * 100, 2) : 0;
}

public class PeriodComparisonBiDto
{
    public string Metric { get; set; } = string.Empty;
    public decimal CurrentPeriodValue { get; set; }
    public decimal PreviousPeriodValue { get; set; }
    public decimal Variance => CurrentPeriodValue - PreviousPeriodValue;
    public decimal VariancePercent => PreviousPeriodValue != 0 ? Math.Round((Variance / Math.Abs(PreviousPeriodValue)) * 100, 2) : 0;
}

public class ProductPerformanceRankDto
{
    public int Rank { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal EstimatedProfit { get; set; }
    public decimal ProfitMargin => Revenue > 0 ? Math.Round((EstimatedProfit / Revenue) * 100, 2) : 0;
}

public class InventoryTurnoverDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal CostOfGoodsSold { get; set; }
    public decimal AverageInventoryValue { get; set; }
    public decimal TurnoverRatio => AverageInventoryValue > 0 ? Math.Round(CostOfGoodsSold / AverageInventoryValue, 2) : 0;
    public int DaysSalesOfInventory => TurnoverRatio > 0 ? (int)Math.Round(365 / TurnoverRatio) : 999;
}

public class PurchaseVsSalesDto
{
    public string Period { get; set; } = string.Empty;
    public decimal SalesAmount { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal NetDifference => SalesAmount - PurchaseAmount;
}

public class SimpleModeDashboardDto
{
    public decimal TodaysSales { get; set; }
    public decimal TodaysCollection { get; set; }
    public int TodaysBills { get; set; }
    public decimal EstimatedGrossProfit { get; set; }
    public bool IsGrossProfitReliable { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public decimal CustomerOutstanding { get; set; }
    public decimal SupplierOutstanding { get; set; }
    public List<ProductWiseSalesDto> TopSellingProducts { get; set; } = new();
    public List<DailySalesDto> DailySalesSummary { get; set; } = new();
    public List<CurrentStockDto> StockAlertItems { get; set; } = new();
}

public class ReportsDashboardDto
{
    public decimal TotalSales { get; set; }
    public decimal TotalPurchase { get; set; }
    public decimal NetSales { get; set; }
    public decimal GrossProfit { get; set; }
    public bool IsGrossProfitReliable { get; set; }
    public decimal TotalOutstandingReceivable { get; set; }
    public decimal TotalOutstandingPayable { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public List<DailyTrendDto> SalesTrend { get; set; } = new();
    public List<DailyTrendDto> PurchaseTrend { get; set; } = new();
    public Dictionary<string, decimal> PaymentModeBreakdown { get; set; } = new();
    public List<ProductPerformanceRankDto> TopProducts { get; set; } = new();
    public List<CustomerWiseSalesDto> TopCustomers { get; set; } = new();
    public List<CategoryWiseSalesDto> CategorySales { get; set; } = new();
    public List<CurrentStockDto> StockAlerts { get; set; } = new();
    public List<AuditTrailReportDto> RecentActivity { get; set; } = new();
}

#endregion
