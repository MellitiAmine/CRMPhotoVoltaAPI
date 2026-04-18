using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.Core
{
    /// <inheritdoc />
    public partial class AddRoleTypeToRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoleType",
                schema: "core",
                table: "Roles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE core."Roles" SET "RoleType" = 1 WHERE lower("Name") = 'admin';
                UPDATE core."Roles" SET "RoleType" = 2 WHERE lower("Name") IN ('manager', 'sales_manager', 'operations_manager');
                UPDATE core."Roles" SET "RoleType" = 3 WHERE lower("Name") IN ('commercial', 'sales_executive', 'account_manager');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleType",
                schema: "core",
                table: "Roles");
        }
    }
}
