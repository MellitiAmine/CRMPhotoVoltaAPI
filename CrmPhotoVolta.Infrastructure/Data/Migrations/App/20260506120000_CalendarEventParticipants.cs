using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class CalendarEventParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "app",
                table: "Events",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Participants",
                schema: "app",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValueSql: "'[]'");

            // Ensure existing rows have a valid default
            migrationBuilder.Sql(
                """
                UPDATE app."Events" SET "Participants" = '[]' WHERE "Participants" IS NULL OR "Participants" = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Description", schema: "app", table: "Events");
            migrationBuilder.DropColumn(name: "Participants", schema: "app", table: "Events");
        }
    }
}
