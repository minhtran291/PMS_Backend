using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fix_SQ_Add_Sales_Staff_Id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SsId",
                table: "SalesQuotations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_SsId",
                table: "SalesQuotations",
                column: "SsId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotations_StaffProfiles_SsId",
                table: "SalesQuotations",
                column: "SsId",
                principalTable: "StaffProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotations_StaffProfiles_SsId",
                table: "SalesQuotations");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotations_SsId",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "SsId",
                table: "SalesQuotations");
        }
    }
}
