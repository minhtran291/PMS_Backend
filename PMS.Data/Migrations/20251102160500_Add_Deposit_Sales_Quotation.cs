using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Deposit_Sales_Quotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepositDueDays",
                table: "SalesQuotations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercent",
                table: "SalesQuotations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepositDueDays",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "DepositPercent",
                table: "SalesQuotations");
        }
    }
}
