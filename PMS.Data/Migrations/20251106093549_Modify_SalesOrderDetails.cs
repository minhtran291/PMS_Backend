using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Modify_SalesOrderDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
        name: "ProductId",
        table: "SalesOrderDetails",
        type: "int",
        nullable: false,
        defaultValue: 0);

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
                onDelete: ReferentialAction.Restrict   // hoặc Cascade nếu bạn muốn
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderDetails_Products_ProductId",
                table: "SalesOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderDetails_ProductId",
                table: "SalesOrderDetails");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "SalesOrderDetails");
        }
    }
}
