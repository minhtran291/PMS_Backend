using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Warehouse_Warehouse_Location : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LotProducts_WarehouselocationID",
                table: "LotProducts");

            migrationBuilder.DropColumn(
                name: "ColumnNo",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "LevelNo",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "RowNo",
                table: "WarehouseLocations");

            migrationBuilder.AlterColumn<bool>(
                name: "Status",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "TINYINT");

            migrationBuilder.AlterColumn<bool>(
                name: "Status",
                table: "WarehouseLocations",
                type: "bit",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "TINYINT");

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "WarehouseLocations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "WarehouselocationID",
                table: "LotProducts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LotProducts_WarehouselocationID",
                table: "LotProducts",
                column: "WarehouselocationID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LotProducts_WarehouselocationID",
                table: "LotProducts");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "WarehouseLocations");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "Warehouses",
                type: "TINYINT",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "WarehouseLocations",
                type: "TINYINT",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<int>(
                name: "ColumnNo",
                table: "WarehouseLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LevelNo",
                table: "WarehouseLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RowNo",
                table: "WarehouseLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "WarehouselocationID",
                table: "LotProducts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_LotProducts_WarehouselocationID",
                table: "LotProducts",
                column: "WarehouselocationID",
                unique: true,
                filter: "[WarehouselocationID] IS NOT NULL");
        }
    }
}
