using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_SalesOrderCode_To_SalesOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "SalesOrders",
                newName: "SalesOrderId");

            migrationBuilder.AddColumn<string>(
                name: "SalesOrderCode",
                table: "SalesOrders",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalesOrderCode",
                table: "SalesOrders");

            migrationBuilder.RenameColumn(
                name: "SalesOrderId",
                table: "SalesOrders",
                newName: "OrderId");
        }
    }
}
