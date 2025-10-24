using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Sales_Quotation_Note : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SqnId",
                table: "SalesQuotations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SalesQuotationNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotationNotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_SqnId",
                table: "SalesQuotations",
                column: "SqnId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotations_SalesQuotationNotes_SqnId",
                table: "SalesQuotations",
                column: "SqnId",
                principalTable: "SalesQuotationNotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotations_SalesQuotationNotes_SqnId",
                table: "SalesQuotations");

            migrationBuilder.DropTable(
                name: "SalesQuotationNotes");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotations_SqnId",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "SqnId",
                table: "SalesQuotations");
        }
    }
}
