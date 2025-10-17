using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class PRFQTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchasingRequestForQuotations",
                columns: table => new
                {
                    PRFQID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MyPhone = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MyAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SupplierID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchasingRequestForQuotations", x => x.PRFQID);
                    table.ForeignKey(
                        name: "FK_PurchasingRequestForQuotations_Suppliers_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchasingRequestForQuotations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchasingRequestProducts",
                columns: table => new
                {
                    PRPID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PRFQID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchasingRequestProducts", x => x.PRPID);
                    table.ForeignKey(
                        name: "FK_PurchasingRequestProducts_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchasingRequestProducts_PurchasingRequestForQuotations_PRFQID",
                        column: x => x.PRFQID,
                        principalTable: "PurchasingRequestForQuotations",
                        principalColumn: "PRFQID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchasingRequestForQuotations_SupplierID",
                table: "PurchasingRequestForQuotations",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasingRequestForQuotations_UserId",
                table: "PurchasingRequestForQuotations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasingRequestProducts_PRFQID",
                table: "PurchasingRequestProducts",
                column: "PRFQID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasingRequestProducts_ProductID",
                table: "PurchasingRequestProducts",
                column: "ProductID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchasingRequestProducts");

            migrationBuilder.DropTable(
                name: "PurchasingRequestForQuotations");
        }
    }
}
