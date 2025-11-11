using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Stock_Export_Order : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockExportOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    CreateBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "TINYINT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockExportOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockExportOrders_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "SalesOrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockExportOrders_Users_CreateBy",
                        column: x => x.CreateBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockExportOrderDetails",
                columns: table => new
                {
                    StockExportOrderId = table.Column<int>(type: "int", nullable: false),
                    LotId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockExportOrderDetails", x => new { x.StockExportOrderId, x.LotId });
                    table.ForeignKey(
                        name: "FK_StockExportOrderDetails_LotProducts_LotId",
                        column: x => x.LotId,
                        principalTable: "LotProducts",
                        principalColumn: "LotID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockExportOrderDetails_StockExportOrders_StockExportOrderId",
                        column: x => x.StockExportOrderId,
                        principalTable: "StockExportOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockExportOrderDetails_LotId",
                table: "StockExportOrderDetails",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockExportOrders_CreateBy",
                table: "StockExportOrders",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockExportOrders_SalesOrderId",
                table: "StockExportOrders",
                column: "SalesOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockExportOrderDetails");

            migrationBuilder.DropTable(
                name: "StockExportOrders");
        }
    }
}
