using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Modified_Relation_Invoice_SalesOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_SalesOrderId",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SalesOrderId",
                table: "Invoices",
                column: "SalesOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_SalesOrderId",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SalesOrderId",
                table: "Invoices",
                column: "SalesOrderId",
                unique: true);
        }
    }
}
