using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class QUOUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PRFQID",
                table: "Quotations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_PRFQID",
                table: "Quotations",
                column: "PRFQID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotations_PurchasingRequestForQuotations_PRFQID",
                table: "Quotations",
                column: "PRFQID",
                principalTable: "PurchasingRequestForQuotations",
                principalColumn: "PRFQID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotations_PurchasingRequestForQuotations_PRFQID",
                table: "Quotations");

            migrationBuilder.DropIndex(
                name: "IX_Quotations_PRFQID",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "PRFQID",
                table: "Quotations");
        }
    }
}
