using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.Core
{
    /// <inheritdoc />
    public partial class DropIsPlatformAdministratorFromUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPlatformAdministrator",
                schema: "core",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPlatformAdministrator",
                schema: "core",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
