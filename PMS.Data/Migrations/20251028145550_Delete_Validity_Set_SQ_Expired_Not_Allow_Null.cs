using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Delete_Validity_Set_SQ_Expired_Not_Allow_Null : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotations_SalesQuotationValidities_SqvId",
                table: "SalesQuotations");

            migrationBuilder.DropTable(
                name: "SalesQuotationValidities");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotations_SqvId",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "SqvId",
                table: "SalesQuotations");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiredDate",
                table: "SalesQuotations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Quotations",
                type: "int",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "PurchasingOrders",
                type: "int",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiredDate",
                table: "SalesQuotations",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "SqvId",
                table: "SalesQuotations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<bool>(
                name: "Status",
                table: "Quotations",
                type: "bit",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "Status",
                table: "PurchasingOrders",
                type: "bit",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "SalesQuotationValidities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Days = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotationValidities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_SqvId",
                table: "SalesQuotations",
                column: "SqvId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotations_SalesQuotationValidities_SqvId",
                table: "SalesQuotations",
                column: "SqvId",
                principalTable: "SalesQuotationValidities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
