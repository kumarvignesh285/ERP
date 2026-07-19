using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddWithGSTToTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WithGST",
                table: "SalesReturns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WithGST",
                table: "SalesQuotations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WithGST",
                table: "SalesOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WithGST",
                table: "SalesInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WithGST",
                table: "PurchaseReturns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WithGST",
                table: "PurchaseOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WithGST",
                table: "PurchaseInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WithGST",
                table: "GoodsReceiptNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WithGST",
                table: "DeliveryChallans",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WithGST",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "WithGST",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "WithGST",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "WithGST",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "WithGST",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "WithGST",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "WithGST",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "WithGST",
                table: "GoodsReceiptNotes");

            migrationBuilder.DropColumn(
                name: "WithGST",
                table: "DeliveryChallans");
        }
    }
}
