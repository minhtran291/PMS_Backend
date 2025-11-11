using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Modified_Entity_In_Customer_Debt_And_Navigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CustomerId",
                table: "CustomerDebts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CustomerProfiles_UserId",
                table: "CustomerProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDebts_CustomerId",
                table: "CustomerDebts",
                column: "CustomerId");


            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDebts_CustomerProfiles_CustomerId",
                table: "CustomerDebts",
                column: "CustomerId",
                principalTable: "CustomerProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDebts_CustomerProfiles_CustomerId",
                table: "CustomerDebts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CustomerProfiles_UserId",
                table: "CustomerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CustomerDebts_CustomerId",
                table: "CustomerDebts");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "CustomerDebts",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);
        }
    }
}
