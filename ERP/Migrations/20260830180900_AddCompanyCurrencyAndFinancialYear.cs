using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCurrencyAndFinancialYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Companies]') AND name = 'AlternatePhone')
                    ALTER TABLE [Companies] ADD [AlternatePhone] nvarchar(20) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Companies]') AND name = 'BusinessType')
                    ALTER TABLE [Companies] ADD [BusinessType] nvarchar(100) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Companies]') AND name = 'Currency')
                    ALTER TABLE [Companies] ADD [Currency] nvarchar(10) NOT NULL DEFAULT N'INR';
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Companies]') AND name = 'FinancialYear')
                    ALTER TABLE [Companies] ADD [FinancialYear] nvarchar(20) NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
