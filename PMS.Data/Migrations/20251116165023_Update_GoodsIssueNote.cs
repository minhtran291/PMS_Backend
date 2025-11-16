using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Update_GoodsIssueNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "GoodsIssueNotes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueNotes_WarehouseId",
                table: "GoodsIssueNotes",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsIssueNotes_Warehouses_WarehouseId",
                table: "GoodsIssueNotes",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsIssueNotes_Warehouses_WarehouseId",
                table: "GoodsIssueNotes");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssueNotes_WarehouseId",
                table: "GoodsIssueNotes");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "GoodsIssueNotes");
        }
    }
}
