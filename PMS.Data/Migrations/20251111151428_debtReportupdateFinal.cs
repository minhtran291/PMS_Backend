using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class debtReportupdateFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "DebtReports");

            migrationBuilder.AddColumn<DateTime>(
                name: "DepositDate",
                table: "PurchasingOrders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<decimal>(
                name: "DebtCeiling",
                table: "PharmacySecretInfor",
                type: "decimal(18,2)",
                nullable: false,
                computedColumnSql: "(([TotalRecieve] - [TotalPaid]) + [Equity]) * 3",
                stored: true,
                comment: "Nợ trần",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldComment: "Trần nợ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepositDate",
                table: "PurchasingOrders");

            migrationBuilder.AlterColumn<decimal>(
                name: "DebtCeiling",
                table: "PharmacySecretInfor",
                type: "decimal(18,2)",
                nullable: false,
                comment: "Trần nợ",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldComputedColumnSql: "(([TotalRecieve] - [TotalPaid]) + [Equity]) * 3",
                oldComment: "Nợ trần");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "DebtReports",
                type: "datetime",
                nullable: true);
        }
    }
}
