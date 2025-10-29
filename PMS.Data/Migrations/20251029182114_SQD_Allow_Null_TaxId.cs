﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class SQD_Allow_Null_TaxId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TaxId",
                table: "SalesQuotaionDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TaxId",
                table: "SalesQuotaionDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
