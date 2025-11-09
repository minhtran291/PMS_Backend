using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class bigupdateinven : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Diff",
                table: "LotProducts");

            migrationBuilder.DropColumn(
                name: "inventoryBy",
                table: "LotProducts");

            migrationBuilder.DropColumn(
                name: "lastedUpdate",
                table: "LotProducts");

            migrationBuilder.DropColumn(
                name: "note",
                table: "LotProducts");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckedDate",
                table: "LotProducts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "InventorySessions",
                columns: table => new
                {
                    InventorySessionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySessions", x => x.InventorySessionID);
                });

            migrationBuilder.CreateTable(
                name: "InventoryHistories",
                columns: table => new
                {
                    InventoryHistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotID = table.Column<int>(type: "int", nullable: false),
                    InventorySessionID = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SystemQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ActualQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: false),
                    InventoryBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryHistories", x => x.InventoryHistoryID);
                    table.ForeignKey(
                        name: "FK_InventoryHistories_InventorySessions_InventorySessionID",
                        column: x => x.InventorySessionID,
                        principalTable: "InventorySessions",
                        principalColumn: "InventorySessionID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryHistories_LotProducts_LotID",
                        column: x => x.LotID,
                        principalTable: "LotProducts",
                        principalColumn: "LotID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHistories_InventorySessionID",
                table: "InventoryHistories",
                column: "InventorySessionID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHistories_LotID",
                table: "InventoryHistories",
                column: "LotID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryHistories");

            migrationBuilder.DropTable(
                name: "InventorySessions");

            migrationBuilder.DropColumn(
                name: "LastCheckedDate",
                table: "LotProducts");

            migrationBuilder.AddColumn<int>(
                name: "Diff",
                table: "LotProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "inventoryBy",
                table: "LotProducts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "lastedUpdate",
                table: "LotProducts",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "note",
                table: "LotProducts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
