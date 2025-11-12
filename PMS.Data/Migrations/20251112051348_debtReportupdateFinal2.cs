using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class debtReportupdateFinal2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "PharmacySecretInfor_Temp",
                columns: table => new
                {
                    PMSID = table.Column<int>(type: "int", nullable: false),
                    Equity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalRecieve = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DebtCeiling = table.Column<decimal>(
                        type: "decimal(18,2)",
                        nullable: false,
                        computedColumnSql: "(([TotalRecieve] - [TotalPaid]) + [Equity]) * 3",
                        stored: true
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacySecretInfor_Temp", x => x.PMSID);
                });

            migrationBuilder.Sql(@"
                INSERT INTO PharmacySecretInfor_Temp (PMSID, Equity, TotalRecieve, TotalPaid)
                SELECT PMSID, Equity, TotalRecieve, TotalPaid
                FROM PharmacySecretInfor
            ");


            migrationBuilder.DropTable(name: "PharmacySecretInfor");

            // 4️⃣ Đổi tên bảng tạm thành bảng chính
            migrationBuilder.RenameTable(
                name: "PharmacySecretInfor_Temp",
                newName: "PharmacySecretInfor"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nếu rollback, tạo lại bảng có IDENTITY
            migrationBuilder.CreateTable(
                name: "PharmacySecretInfor_Temp",
                columns: table => new
                {
                    PMSID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Equity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalRecieve = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DebtCeiling = table.Column<decimal>(
                        type: "decimal(18,2)",
                        nullable: false,
                        computedColumnSql: "(([TotalRecieve] - [TotalPaid]) + [Equity]) * 3",
                        stored: true
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacySecretInfor_Temp", x => x.PMSID);
                });

            migrationBuilder.Sql(@"
                INSERT INTO PharmacySecretInfor_Temp (Equity, TotalRecieve, TotalPaid)
                SELECT Equity, TotalRecieve, TotalPaid
                FROM PharmacySecretInfor
            ");

            migrationBuilder.DropTable(name: "PharmacySecretInfor");

            migrationBuilder.RenameTable(
                name: "PharmacySecretInfor_Temp",
                newName: "PharmacySecretInfor"
            );
        }
    }
}
