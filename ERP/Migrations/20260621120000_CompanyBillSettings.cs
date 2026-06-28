using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations;

public partial class CompanyBillSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BillFooterNote",
            table: "Companies",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BillType",
            table: "Companies",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "Tax Invoice");

        migrationBuilder.AddColumn<int>(
            name: "PurchaseBillNextNumber",
            table: "Companies",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "PurchaseBillPrefix",
            table: "Companies",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "PINV-");

        migrationBuilder.AddColumn<int>(
            name: "PurchaseBillStartNumber",
            table: "Companies",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "SalesBillNextNumber",
            table: "Companies",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "SalesBillPrefix",
            table: "Companies",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "INV-");

        migrationBuilder.AddColumn<int>(
            name: "SalesBillStartNumber",
            table: "Companies",
            type: "int",
            nullable: false,
            defaultValue: 1);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BillFooterNote", table: "Companies");
        migrationBuilder.DropColumn(name: "BillType", table: "Companies");
        migrationBuilder.DropColumn(name: "PurchaseBillNextNumber", table: "Companies");
        migrationBuilder.DropColumn(name: "PurchaseBillPrefix", table: "Companies");
        migrationBuilder.DropColumn(name: "PurchaseBillStartNumber", table: "Companies");
        migrationBuilder.DropColumn(name: "SalesBillNextNumber", table: "Companies");
        migrationBuilder.DropColumn(name: "SalesBillPrefix", table: "Companies");
        migrationBuilder.DropColumn(name: "SalesBillStartNumber", table: "Companies");
    }
}
