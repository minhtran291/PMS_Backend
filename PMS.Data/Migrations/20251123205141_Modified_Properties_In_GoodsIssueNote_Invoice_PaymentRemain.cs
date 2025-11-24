using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Modified_Properties_In_GoodsIssueNote_Invoice_PaymentRemain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_SalesOrders_SalesOrderId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRemains_GoodsIssueNotes_GoodsIssueNoteId",
                table: "PaymentRemains");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRemains_GoodsIssueNoteId",
                table: "PaymentRemains");

            migrationBuilder.DropColumn(
                name: "GoodsIssueNoteId",
                table: "PaymentRemains");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "PaymentRemains",
                newName: "VNPayStatus");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaidFullAt",
                table: "SalesOrders",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceCode",
                table: "PaymentRemains",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "PaymentRemains",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "PaymentRemains",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PaymentStatus",
                table: "Invoices",
                type: "TINYINT",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRemains_InvoiceId",
                table: "PaymentRemains",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_SalesOrders_SalesOrderId",
                table: "Invoices",
                column: "SalesOrderId",
                principalTable: "SalesOrders",
                principalColumn: "SalesOrderId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRemains_Invoices_InvoiceId",
                table: "PaymentRemains",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_SalesOrders_SalesOrderId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRemains_Invoices_InvoiceId",
                table: "PaymentRemains");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRemains_InvoiceId",
                table: "PaymentRemains");

            migrationBuilder.DropColumn(
                name: "InvoiceCode",
                table: "PaymentRemains");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "PaymentRemains");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "PaymentRemains");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "VNPayStatus",
                table: "PaymentRemains",
                newName: "Status");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaidFullAt",
                table: "SalesOrders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GoodsIssueNoteId",
                table: "PaymentRemains",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRemains_GoodsIssueNoteId",
                table: "PaymentRemains",
                column: "GoodsIssueNoteId",
                unique: true,
                filter: "[GoodsIssueNoteId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_SalesOrders_SalesOrderId",
                table: "Invoices",
                column: "SalesOrderId",
                principalTable: "SalesOrders",
                principalColumn: "SalesOrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRemains_GoodsIssueNotes_GoodsIssueNoteId",
                table: "PaymentRemains",
                column: "GoodsIssueNoteId",
                principalTable: "GoodsIssueNotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
