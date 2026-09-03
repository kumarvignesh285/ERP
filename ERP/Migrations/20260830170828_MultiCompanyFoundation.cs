using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class MultiCompanyFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Taxes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Suppliers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "StockTransfers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "StockTransactions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "StockAdjustments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "ScreenPermissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "SalesReturns",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "SalesQuotations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "SalesInvoices",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "PurchaseReturns",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "PurchaseOrders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "PurchaseInvoices",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "PhysicalStockVerifications",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "PaymentModes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Opportunities",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Ledgers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Leads",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "GoodsReceiptNotes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "FollowUps",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "DeliveryChallans",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyCode",
                table: "Companies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Companies",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "INR");

            migrationBuilder.AddColumn<string>(
                name: "FinancialYear",
                table: "Companies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Brands",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Banks",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "AccountGroups",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_CompanyId",
                table: "Warehouses",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_CompanyId_VoucherNumber",
                table: "Vouchers",
                columns: new[] { "CompanyId", "VoucherNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Units_CompanyId",
                table: "Units",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Taxes_CompanyId",
                table: "Taxes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CompanyId_SupplierCode",
                table: "Suppliers",
                columns: new[] { "CompanyId", "SupplierCode" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_CompanyId",
                table: "StockTransfers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_CompanyId",
                table: "StockTransactions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_CompanyId",
                table: "StockAdjustments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreenPermissions_CompanyId",
                table: "ScreenPermissions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_CompanyId",
                table: "SalesReturns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_CompanyId",
                table: "SalesQuotations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CompanyId_OrderNumber",
                table: "SalesOrders",
                columns: new[] { "CompanyId", "OrderNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CompanyId_InvoiceNumber",
                table: "SalesInvoices",
                columns: new[] { "CompanyId", "InvoiceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_CompanyId",
                table: "PurchaseReturns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CompanyId_OrderNumber",
                table: "PurchaseOrders",
                columns: new[] { "CompanyId", "OrderNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_CompanyId_InvoiceNumber",
                table: "PurchaseInvoices",
                columns: new[] { "CompanyId", "InvoiceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_ProductCode",
                table: "Products",
                columns: new[] { "CompanyId", "ProductCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalStockVerifications_CompanyId",
                table: "PhysicalStockVerifications",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentModes_CompanyId",
                table: "PaymentModes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_CompanyId",
                table: "Opportunities",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CompanyId",
                table: "Notifications",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Ledgers_CompanyId",
                table: "Ledgers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_CompanyId",
                table: "Leads",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptNotes_CompanyId",
                table: "GoodsReceiptNotes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_CompanyId",
                table: "FollowUps",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId",
                table: "Employees",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_CompanyId",
                table: "DeliveryChallans",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId_CustomerCode",
                table: "Customers",
                columns: new[] { "CompanyId", "CustomerCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyCode",
                table: "Companies",
                column: "CompanyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CompanyId",
                table: "Categories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_CompanyId",
                table: "Brands",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Banks_CompanyId",
                table: "Banks",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CompanyId",
                table: "AspNetUsers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountGroups_CompanyId",
                table: "AccountGroups",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountGroups_Companies_CompanyId",
                table: "AccountGroups",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Companies_CompanyId",
                table: "AspNetUsers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Banks_Companies_CompanyId",
                table: "Banks",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Brands_Companies_CompanyId",
                table: "Brands",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Companies_CompanyId",
                table: "Categories",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Companies_CompanyId",
                table: "Customers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryChallans_Companies_CompanyId",
                table: "DeliveryChallans",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Companies_CompanyId",
                table: "Employees",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FollowUps_Companies_CompanyId",
                table: "FollowUps",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptNotes_Companies_CompanyId",
                table: "GoodsReceiptNotes",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Companies_CompanyId",
                table: "Leads",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ledgers_Companies_CompanyId",
                table: "Ledgers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Companies_CompanyId",
                table: "Notifications",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Opportunities_Companies_CompanyId",
                table: "Opportunities",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentModes_Companies_CompanyId",
                table: "PaymentModes",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhysicalStockVerifications_Companies_CompanyId",
                table: "PhysicalStockVerifications",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Companies_CompanyId",
                table: "Products",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_Companies_CompanyId",
                table: "PurchaseInvoices",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Companies_CompanyId",
                table: "PurchaseOrders",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReturns_Companies_CompanyId",
                table: "PurchaseReturns",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Companies_CompanyId",
                table: "SalesInvoices",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Companies_CompanyId",
                table: "SalesOrders",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotations_Companies_CompanyId",
                table: "SalesQuotations",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesReturns_Companies_CompanyId",
                table: "SalesReturns",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScreenPermissions_Companies_CompanyId",
                table: "ScreenPermissions",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustments_Companies_CompanyId",
                table: "StockAdjustments",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_Companies_CompanyId",
                table: "StockTransactions",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_Companies_CompanyId",
                table: "StockTransfers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Companies_CompanyId",
                table: "Suppliers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Taxes_Companies_CompanyId",
                table: "Taxes",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Companies_CompanyId",
                table: "Units",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Companies_CompanyId",
                table: "Vouchers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Companies_CompanyId",
                table: "Warehouses",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountGroups_Companies_CompanyId",
                table: "AccountGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Companies_CompanyId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Banks_Companies_CompanyId",
                table: "Banks");

            migrationBuilder.DropForeignKey(
                name: "FK_Brands_Companies_CompanyId",
                table: "Brands");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Companies_CompanyId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Companies_CompanyId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryChallans_Companies_CompanyId",
                table: "DeliveryChallans");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Companies_CompanyId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_FollowUps_Companies_CompanyId",
                table: "FollowUps");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptNotes_Companies_CompanyId",
                table: "GoodsReceiptNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Companies_CompanyId",
                table: "Leads");

            migrationBuilder.DropForeignKey(
                name: "FK_Ledgers_Companies_CompanyId",
                table: "Ledgers");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Companies_CompanyId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Opportunities_Companies_CompanyId",
                table: "Opportunities");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentModes_Companies_CompanyId",
                table: "PaymentModes");

            migrationBuilder.DropForeignKey(
                name: "FK_PhysicalStockVerifications_Companies_CompanyId",
                table: "PhysicalStockVerifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Companies_CompanyId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoices_Companies_CompanyId",
                table: "PurchaseInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Companies_CompanyId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturns_Companies_CompanyId",
                table: "PurchaseReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Companies_CompanyId",
                table: "SalesInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Companies_CompanyId",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotations_Companies_CompanyId",
                table: "SalesQuotations");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesReturns_Companies_CompanyId",
                table: "SalesReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_ScreenPermissions_Companies_CompanyId",
                table: "ScreenPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustments_Companies_CompanyId",
                table: "StockAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_Companies_CompanyId",
                table: "StockTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_Companies_CompanyId",
                table: "StockTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Companies_CompanyId",
                table: "Suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_Taxes_Companies_CompanyId",
                table: "Taxes");

            migrationBuilder.DropForeignKey(
                name: "FK_Units_Companies_CompanyId",
                table: "Units");

            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Companies_CompanyId",
                table: "Vouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Companies_CompanyId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_CompanyId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_CompanyId_VoucherNumber",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Units_CompanyId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Taxes_CompanyId",
                table: "Taxes");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_CompanyId_SupplierCode",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_CompanyId",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_CompanyId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustments_CompanyId",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_ScreenPermissions_CompanyId",
                table: "ScreenPermissions");

            migrationBuilder.DropIndex(
                name: "IX_SalesReturns_CompanyId",
                table: "SalesReturns");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotations_CompanyId",
                table: "SalesQuotations");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_CompanyId_OrderNumber",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_CompanyId_InvoiceNumber",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturns_CompanyId",
                table: "PurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CompanyId_OrderNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoices_CompanyId_InvoiceNumber",
                table: "PurchaseInvoices");

            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId_ProductCode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalStockVerifications_CompanyId",
                table: "PhysicalStockVerifications");

            migrationBuilder.DropIndex(
                name: "IX_PaymentModes_CompanyId",
                table: "PaymentModes");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_CompanyId",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_CompanyId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Ledgers_CompanyId",
                table: "Ledgers");

            migrationBuilder.DropIndex(
                name: "IX_Leads_CompanyId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptNotes_CompanyId",
                table: "GoodsReceiptNotes");

            migrationBuilder.DropIndex(
                name: "IX_FollowUps_CompanyId",
                table: "FollowUps");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CompanyId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryChallans_CompanyId",
                table: "DeliveryChallans");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CompanyId_CustomerCode",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CompanyCode",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CompanyId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Brands_CompanyId",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Banks_CompanyId",
                table: "Banks");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CompanyId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AccountGroups_CompanyId",
                table: "AccountGroups");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Taxes");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ScreenPermissions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PhysicalStockVerifications");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PaymentModes");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Ledgers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "GoodsReceiptNotes");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "FollowUps");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FinancialYear",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Banks");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AccountGroups");

            migrationBuilder.AlterColumn<string>(
                name: "CompanyCode",
                table: "Companies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
