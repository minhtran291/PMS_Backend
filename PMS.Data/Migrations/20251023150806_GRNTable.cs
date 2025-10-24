using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class GRNTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoodReceiptNotes",
                columns: table => new
                {
                    GRNID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreateBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    POID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodReceiptNotes", x => x.GRNID);
                    table.ForeignKey(
                        name: "FK_GoodReceiptNotes_PurchasingOrders_POID",
                        column: x => x.POID,
                        principalTable: "PurchasingOrders",
                        principalColumn: "POID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodReceiptNoteDetails",
                columns: table => new
                {
                    GRNDID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    GRNID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodReceiptNoteDetails", x => x.GRNDID);
                    table.ForeignKey(
                        name: "FK_GoodReceiptNoteDetails_GoodReceiptNotes_GRNID",
                        column: x => x.GRNID,
                        principalTable: "GoodReceiptNotes",
                        principalColumn: "GRNID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodReceiptNoteDetails_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodReceiptNoteDetails_GRNID",
                table: "GoodReceiptNoteDetails",
                column: "GRNID");

            migrationBuilder.CreateIndex(
                name: "IX_GoodReceiptNoteDetails_ProductID",
                table: "GoodReceiptNoteDetails",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_GoodReceiptNotes_POID",
                table: "GoodReceiptNotes",
                column: "POID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodReceiptNoteDetails");

            migrationBuilder.DropTable(
                name: "GoodReceiptNotes");
        }
    }
}
