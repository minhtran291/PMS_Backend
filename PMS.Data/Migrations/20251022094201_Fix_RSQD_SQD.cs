using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fix_RSQD_SQD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotaionDetails_SalesQuotations_SalesQuotationId",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotaionDetails_SalesQuotationId",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropColumn(
                name: "SalesQuotationId",
                table: "SalesQuotaionDetails");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotaionDetails_SalesQuotations_SqId",
                table: "SalesQuotaionDetails",
                column: "SqId",
                principalTable: "SalesQuotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotaionDetails_SalesQuotations_SqId",
                table: "SalesQuotaionDetails");

            migrationBuilder.AddColumn<int>(
                name: "SalesQuotationId",
                table: "SalesQuotaionDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotaionDetails_SalesQuotationId",
                table: "SalesQuotaionDetails",
                column: "SalesQuotationId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotaionDetails_SalesQuotations_SalesQuotationId",
                table: "SalesQuotaionDetails",
                column: "SalesQuotationId",
                principalTable: "SalesQuotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
