using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Modified_Realtion_InvoiceDetail_GoodsIssueNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoiceDetails_GoodsIssueNoteId",
                table: "InvoiceDetails");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDetails_GoodsIssueNoteId",
                table: "InvoiceDetails",
                column: "GoodsIssueNoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoiceDetails_GoodsIssueNoteId",
                table: "InvoiceDetails");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDetails_GoodsIssueNoteId",
                table: "InvoiceDetails",
                column: "GoodsIssueNoteId",
                unique: true);
        }
    }
}
