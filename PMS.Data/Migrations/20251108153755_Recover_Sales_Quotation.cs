using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Recover_Sales_Quotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesQuotaionDetails",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropColumn(
                name: "ExpectedExpiryNote",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropColumn(
                name: "SalesPrice",
                table: "SalesQuotaionDetails");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "SalesQuotaionDetails",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "LotId",
                table: "SalesQuotaionDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesQuotaionDetails",
                table: "SalesQuotaionDetails",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotaionDetails_LotId",
                table: "SalesQuotaionDetails",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotaionDetails_SqId",
                table: "SalesQuotaionDetails",
                column: "SqId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotaionDetails_LotProducts_LotId",
                table: "SalesQuotaionDetails",
                column: "LotId",
                principalTable: "LotProducts",
                principalColumn: "LotID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotaionDetails_LotProducts_LotId",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesQuotaionDetails",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotaionDetails_LotId",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotaionDetails_SqId",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropColumn(
                name: "LotId",
                table: "SalesQuotaionDetails");

            migrationBuilder.AddColumn<string>(
                name: "ExpectedExpiryNote",
                table: "SalesQuotaionDetails",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "SalesPrice",
                table: "SalesQuotaionDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesQuotaionDetails",
                table: "SalesQuotaionDetails",
                columns: new[] { "SqId", "ProductId" });
        }
    }
}
