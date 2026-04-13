using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.Core
{
    /// <inheritdoc />
    public partial class EnforceRoleUniquenessPerSociety : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Roles_SocietyId_Name",
                schema: "core",
                table: "Roles",
                columns: new[] { "SocietyId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_SocietyId_Name",
                schema: "core",
                table: "Roles");
        }
    }
}
