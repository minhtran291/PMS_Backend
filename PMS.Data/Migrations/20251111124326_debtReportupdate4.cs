using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class debtReportupdate4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebtCeiling",
                table: "DebtReports");

            migrationBuilder.DropColumn(
                name: "TotalPaid",
                table: "DebtReports");

            migrationBuilder.CreateTable(
                name: "PharmacySecretInfor",
                columns: table => new
                {
                    PMSID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Equity = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Vốn chủ sở hữu"),
                    TotalRecieve = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Tổng thu"),
                    TotalPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Tổng chi"),
                    DebtCeiling = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Trần nợ")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacySecretInfor", x => x.PMSID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PharmacySecretInfor");

            migrationBuilder.AddColumn<decimal>(
                name: "DebtCeiling",
                table: "DebtReports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPaid",
                table: "DebtReports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
