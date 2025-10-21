using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class LotProduct_Warehouse_update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LotID",
                table: "WarehouseLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WarehouselocationID",
                table: "LotProducts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LotProducts_WarehouselocationID",
                table: "LotProducts",
                column: "WarehouselocationID",
                unique: true,
                filter: "[WarehouselocationID] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_LotProducts_WarehouseLocations_WarehouselocationID",
                table: "LotProducts",
                column: "WarehouselocationID",
                principalTable: "WarehouseLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LotProducts_WarehouseLocations_WarehouselocationID",
                table: "LotProducts");

            migrationBuilder.DropIndex(
                name: "IX_LotProducts_WarehouselocationID",
                table: "LotProducts");

            migrationBuilder.DropColumn(
                name: "LotID",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "WarehouselocationID",
                table: "LotProducts");
        }
    }
}
