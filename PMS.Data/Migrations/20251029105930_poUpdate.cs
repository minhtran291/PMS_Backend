using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class poUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductID",
                table: "PurchasingOrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PurchasingOrderDetails_ProductID",
                table: "PurchasingOrderDetails",
                column: "ProductID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchasingOrderDetails_Products_ProductID",
                table: "PurchasingOrderDetails",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchasingOrderDetails_Products_ProductID",
                table: "PurchasingOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_PurchasingOrderDetails_ProductID",
                table: "PurchasingOrderDetails");

            migrationBuilder.DropColumn(
                name: "ProductID",
                table: "PurchasingOrderDetails");
        }
    }
}
