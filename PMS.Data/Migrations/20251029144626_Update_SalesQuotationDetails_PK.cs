using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Update_SalesQuotationDetails_PK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotaionDetails_LotProducts_LotId",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotaionDetails_TaxPolicies_TaxId",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesQuotaionDetails",
                table: "SalesQuotaionDetails");

            migrationBuilder.AlterColumn<int>(
                name: "LotId",
                table: "SalesQuotaionDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "SalesQuotaionDetails",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "SalesQuotaionDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "SalesQuotaionDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesQuotaionDetails",
                table: "SalesQuotaionDetails",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotaionDetails_ProductId",
                table: "SalesQuotaionDetails",
                column: "ProductId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotaionDetails_Products_ProductId",
                table: "SalesQuotaionDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotaionDetails_TaxPolicies_TaxId",
                table: "SalesQuotaionDetails",
                column: "TaxId",
                principalTable: "TaxPolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotaionDetails_LotProducts_LotId",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotaionDetails_Products_ProductId",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotaionDetails_TaxPolicies_TaxId",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesQuotaionDetails",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotaionDetails_ProductId",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotaionDetails_SqId",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "SalesQuotaionDetails");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "SalesQuotaionDetails");

            migrationBuilder.AlterColumn<int>(
                name: "LotId",
                table: "SalesQuotaionDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesQuotaionDetails",
                table: "SalesQuotaionDetails",
                columns: new[] { "SqId", "LotId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotaionDetails_LotProducts_LotId",
                table: "SalesQuotaionDetails",
                column: "LotId",
                principalTable: "LotProducts",
                principalColumn: "LotID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotaionDetails_TaxPolicies_TaxId",
                table: "SalesQuotaionDetails",
                column: "TaxId",
                principalTable: "TaxPolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
