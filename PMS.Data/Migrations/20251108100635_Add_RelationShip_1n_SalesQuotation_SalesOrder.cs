using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_RelationShip_1n_SalesQuotation_SalesOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SalesQuotationId",
                table: "SalesOrders",
                column: "SalesQuotationId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_SalesQuotations_SalesQuotationId",
                table: "SalesOrders",
                column: "SalesQuotationId",
                principalTable: "SalesQuotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_SalesQuotations_SalesQuotationId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_SalesQuotationId",
                table: "SalesOrders");
        }
    }
}
