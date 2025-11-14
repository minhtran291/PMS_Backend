using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Delete_ProductId_Sales_Order : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderDetails_Products_ProductId",
                table: "SalesOrderDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesOrderDetails",
                table: "SalesOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderDetails_ProductId",
                table: "SalesOrderDetails");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "SalesOrderDetails");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesOrderDetails",
                table: "SalesOrderDetails",
                columns: new[] { "SalesOrderId", "LotId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesOrderDetails",
                table: "SalesOrderDetails");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "SalesOrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesOrderDetails",
                table: "SalesOrderDetails",
                columns: new[] { "SalesOrderId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderDetails_ProductId",
                table: "SalesOrderDetails",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderDetails_Products_ProductId",
                table: "SalesOrderDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
