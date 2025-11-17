using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Split_Status_In_SalesOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "SalesOrders",
                newName: "SalesOrderStatus");

            migrationBuilder.AddColumn<byte>(
                name: "PaymentStatus",
                table: "SalesOrders",
                type: "TINYINT",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "SalesOrders");

            migrationBuilder.RenameColumn(
                name: "SalesOrderStatus",
                table: "SalesOrders",
                newName: "Status");
        }
    }
}
