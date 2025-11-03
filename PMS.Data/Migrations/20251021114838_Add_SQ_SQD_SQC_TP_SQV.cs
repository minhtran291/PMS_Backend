using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_SQ_SQD_SQC_TP_SQV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesQuotationValidities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Days = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotationValidities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RsqId = table.Column<int>(type: "int", nullable: false),
                    SqvId = table.Column<int>(type: "int", nullable: false),
                    QuotationCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    QuotationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "TINYINT", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_RequestSalesQuotations_RsqId",
                        column: x => x.RsqId,
                        principalTable: "RequestSalesQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_SalesQuotationValidities_SqvId",
                        column: x => x.SqvId,
                        principalTable: "SalesQuotationValidities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuotaionDetails",
                columns: table => new
                {
                    SqId = table.Column<int>(type: "int", nullable: false),
                    LotId = table.Column<int>(type: "int", nullable: false),
                    TaxId = table.Column<int>(type: "int", nullable: false),
                    SalesQuotationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotaionDetails", x => new { x.SqId, x.LotId });
                    table.ForeignKey(
                        name: "FK_SalesQuotaionDetails_LotProducts_LotId",
                        column: x => x.LotId,
                        principalTable: "LotProducts",
                        principalColumn: "LotID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesQuotaionDetails_SalesQuotations_SalesQuotationId",
                        column: x => x.SalesQuotationId,
                        principalTable: "SalesQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesQuotaionDetails_TaxPolicies_TaxId",
                        column: x => x.TaxId,
                        principalTable: "TaxPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuotationComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SqId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotationComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesQuotationComments_SalesQuotations_SqId",
                        column: x => x.SqId,
                        principalTable: "SalesQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotationComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotaionDetails_LotId",
                table: "SalesQuotaionDetails",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotaionDetails_SalesQuotationId",
                table: "SalesQuotaionDetails",
                column: "SalesQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotaionDetails_TaxId",
                table: "SalesQuotaionDetails",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationComments_SqId",
                table: "SalesQuotationComments",
                column: "SqId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationComments_UserId",
                table: "SalesQuotationComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_RsqId",
                table: "SalesQuotations",
                column: "RsqId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_SqvId",
                table: "SalesQuotations",
                column: "SqvId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesQuotaionDetails");

            migrationBuilder.DropTable(
                name: "SalesQuotationComments");

            migrationBuilder.DropTable(
                name: "TaxPolicies");

            migrationBuilder.DropTable(
                name: "SalesQuotations");

            migrationBuilder.DropTable(
                name: "SalesQuotationValidities");
        }
    }
}
