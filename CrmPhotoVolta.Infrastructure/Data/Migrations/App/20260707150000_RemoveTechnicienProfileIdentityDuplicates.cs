using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260707150000_RemoveTechnicienProfileIdentityDuplicates")]
    public partial class RemoveTechnicienProfileIdentityDuplicates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE core."Users" u
                SET
                    "FullName" = TRIM(CONCAT(tp."FirstName", ' ', tp."LastName")),
                    "Email"    = tp."Email",
                    "Phone"    = tp."Phone"
                FROM app."TechnicienProfiles" tp
                WHERE u."Id" = tp."UserId"
                  AND NOT tp."IsDeleted"
                  AND NOT u."IsDeleted";
                """);

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "app",
                table: "TechnicienProfiles");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "app",
                table: "TechnicienProfiles");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "app",
                table: "TechnicienProfiles");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "app",
                table: "TechnicienProfiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "app",
                table: "TechnicienProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "app",
                table: "TechnicienProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "app",
                table: "TechnicienProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "app",
                table: "TechnicienProfiles",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE app."TechnicienProfiles" tp
                SET
                    "FirstName" = SPLIT_PART(u."FullName", ' ', 1),
                    "LastName"  = NULLIF(TRIM(SUBSTRING(u."FullName" FROM POSITION(' ' IN u."FullName") + 1)), ''),
                    "Email"     = u."Email",
                    "Phone"     = u."Phone"
                FROM core."Users" u
                WHERE u."Id" = tp."UserId"
                  AND NOT tp."IsDeleted"
                  AND NOT u."IsDeleted";
                """);
        }
    }
}
