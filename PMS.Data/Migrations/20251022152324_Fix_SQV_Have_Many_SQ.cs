using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fix_SQV_Have_Many_SQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesQuotations_SqvId",
                table: "SalesQuotations");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_SqvId",
                table: "SalesQuotations",
                column: "SqvId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesQuotations_SqvId",
                table: "SalesQuotations");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_SqvId",
                table: "SalesQuotations",
                column: "SqvId",
                unique: true);
        }
    }
}
