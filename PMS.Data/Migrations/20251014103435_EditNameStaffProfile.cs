using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class EditNameStaffProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesStaffProfiles_Users_UserId",
                table: "SalesStaffProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesStaffProfiles",
                table: "SalesStaffProfiles");

            migrationBuilder.RenameTable(
                name: "SalesStaffProfiles",
                newName: "StaffProfiles");

            migrationBuilder.RenameIndex(
                name: "IX_SalesStaffProfiles_UserId",
                table: "StaffProfiles",
                newName: "IX_StaffProfiles_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaffProfiles",
                table: "StaffProfiles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffProfiles_Users_UserId",
                table: "StaffProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffProfiles_Users_UserId",
                table: "StaffProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StaffProfiles",
                table: "StaffProfiles");

            migrationBuilder.RenameTable(
                name: "StaffProfiles",
                newName: "SalesStaffProfiles");

            migrationBuilder.RenameIndex(
                name: "IX_StaffProfiles_UserId",
                table: "SalesStaffProfiles",
                newName: "IX_SalesStaffProfiles_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesStaffProfiles",
                table: "SalesStaffProfiles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesStaffProfiles_Users_UserId",
                table: "SalesStaffProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
