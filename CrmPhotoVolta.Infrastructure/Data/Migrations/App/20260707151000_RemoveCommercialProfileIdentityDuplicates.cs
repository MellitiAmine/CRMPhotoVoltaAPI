using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260707151000_RemoveCommercialProfileIdentityDuplicates")]
    public partial class RemoveCommercialProfileIdentityDuplicates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE core."Users" u
                SET
                    "FullName" = TRIM(CONCAT(cp."FirstName", ' ', cp."LastName")),
                    "Email"    = cp."Email",
                    "Phone"    = cp."Phone"
                FROM app."CommercialProfiles" cp
                WHERE u."Id" = cp."UserId"
                  AND NOT cp."IsDeleted"
                  AND NOT u."IsDeleted";
                """);

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "app",
                table: "CommercialProfiles");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "app",
                table: "CommercialProfiles");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "app",
                table: "CommercialProfiles");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "app",
                table: "CommercialProfiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "app",
                table: "CommercialProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "app",
                table: "CommercialProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "app",
                table: "CommercialProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "app",
                table: "CommercialProfiles",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE app."CommercialProfiles" cp
                SET
                    "FirstName" = SPLIT_PART(u."FullName", ' ', 1),
                    "LastName"  = NULLIF(TRIM(SUBSTRING(u."FullName" FROM POSITION(' ' IN u."FullName") + 1)), ''),
                    "Email"     = u."Email",
                    "Phone"     = u."Phone"
                FROM core."Users" u
                WHERE u."Id" = cp."UserId"
                  AND NOT cp."IsDeleted"
                  AND NOT u."IsDeleted";
                """);
        }
    }
}
