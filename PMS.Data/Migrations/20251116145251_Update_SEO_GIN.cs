using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Update_SEO_GIN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GoodsIssueNotes_StockExportOrderId",
                table: "GoodsIssueNotes");

            migrationBuilder.AddColumn<string>(
                name: "StockExportOrderCode",
                table: "StockExportOrders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GoodsIssueNoteCode",
                table: "GoodsIssueNotes",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "warehouseID",
                table: "GoodReceiptNotes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueNotes_StockExportOrderId",
                table: "GoodsIssueNotes",
                column: "StockExportOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GoodsIssueNotes_StockExportOrderId",
                table: "GoodsIssueNotes");

            migrationBuilder.DropColumn(
                name: "StockExportOrderCode",
                table: "StockExportOrders");

            migrationBuilder.DropColumn(
                name: "GoodsIssueNoteCode",
                table: "GoodsIssueNotes");

            migrationBuilder.AlterColumn<int>(
                name: "warehouseID",
                table: "GoodReceiptNotes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueNotes_StockExportOrderId",
                table: "GoodsIssueNotes",
                column: "StockExportOrderId",
                unique: true);
        }
    }
}
