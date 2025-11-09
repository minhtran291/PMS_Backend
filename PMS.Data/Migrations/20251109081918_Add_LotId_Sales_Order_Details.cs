using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_LotId_Sales_Order_Details : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LotId",
                table: "SalesOrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderDetails_LotId",
                table: "SalesOrderDetails",
                column: "LotId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderDetails_LotProducts_LotId",
                table: "SalesOrderDetails",
                column: "LotId",
                principalTable: "LotProducts",
                principalColumn: "LotID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderDetails_LotProducts_LotId",
                table: "SalesOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderDetails_LotId",
                table: "SalesOrderDetails");

            migrationBuilder.DropColumn(
                name: "LotId",
                table: "SalesOrderDetails");
        }
    }
}
