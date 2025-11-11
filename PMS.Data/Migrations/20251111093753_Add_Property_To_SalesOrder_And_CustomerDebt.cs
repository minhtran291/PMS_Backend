using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Property_To_SalesOrder_And_CustomerDebt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerDebts_SalesOrderId",
                table: "CustomerDebts");

            migrationBuilder.AddColumn<DateTime>(
                name: "SalesOrderExpiredDate",
                table: "SalesOrders",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte>(
                name: "status",
                table: "CustomerDebts",
                type: "TINYINT",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDebts_SalesOrderId",
                table: "CustomerDebts",
                column: "SalesOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerDebts_SalesOrderId",
                table: "CustomerDebts");

            migrationBuilder.DropColumn(
                name: "SalesOrderExpiredDate",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "status",
                table: "CustomerDebts");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDebts_SalesOrderId",
                table: "CustomerDebts",
                column: "SalesOrderId");
        }
    }
}
