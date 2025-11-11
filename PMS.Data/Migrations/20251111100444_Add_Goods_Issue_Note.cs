using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Goods_Issue_Note : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoodsIssueNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockExportOrderId = table.Column<int>(type: "int", nullable: false),
                    CreateBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExportedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<byte>(type: "TINYINT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsIssueNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsIssueNotes_StockExportOrders_StockExportOrderId",
                        column: x => x.StockExportOrderId,
                        principalTable: "StockExportOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsIssueNotes_Users_CreateBy",
                        column: x => x.CreateBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsIssueNoteDetails",
                columns: table => new
                {
                    GoodsIssueNoteId = table.Column<int>(type: "int", nullable: false),
                    LotId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsIssueNoteDetails", x => new { x.GoodsIssueNoteId, x.LotId });
                    table.ForeignKey(
                        name: "FK_GoodsIssueNoteDetails_GoodsIssueNotes_GoodsIssueNoteId",
                        column: x => x.GoodsIssueNoteId,
                        principalTable: "GoodsIssueNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsIssueNoteDetails_LotProducts_LotId",
                        column: x => x.LotId,
                        principalTable: "LotProducts",
                        principalColumn: "LotID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueNoteDetails_LotId",
                table: "GoodsIssueNoteDetails",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueNotes_CreateBy",
                table: "GoodsIssueNotes",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueNotes_StockExportOrderId",
                table: "GoodsIssueNotes",
                column: "StockExportOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodsIssueNoteDetails");

            migrationBuilder.DropTable(
                name: "GoodsIssueNotes");
        }
    }
}
