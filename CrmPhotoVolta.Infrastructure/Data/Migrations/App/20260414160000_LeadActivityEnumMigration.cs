using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class LeadActivityEnumMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Temperature",
                schema: "app",
                table: "Leads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                schema: "app",
                table: "Leads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                schema: "app",
                table: "LeadActivities",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE app."LeadActivities" ALTER COLUMN "Type" TYPE integer USING (
                  CASE trim(COALESCE("Type"::text, ''))
                    WHEN 'Call' THEN 1
                    WHEN 'PhoneCall' THEN 1
                    WHEN 'WhatsApp' THEN 2
                    WHEN 'WhatsAppSms' THEN 2
                    WHEN 'Sms' THEN 3
                    WHEN 'MeetingScheduled' THEN 4
                    WHEN 'Meeting' THEN 4
                    WHEN 'TechnicalVisit' THEN 5
                    WHEN 'SiteVisit' THEN 5
                    WHEN 'InfoRequest' THEN 10
                    WHEN 'Info' THEN 10
                    WHEN 'QuoteRequest' THEN 11
                    WHEN 'Quote' THEN 11
                    WHEN 'Negotiation' THEN 12
                    WHEN 'StrongBuying' THEN 13
                    WHEN 'Assignment' THEN 100
                    WHEN 'StatusChange' THEN 101
                    WHEN 'Note' THEN 102
                    WHEN 'Converted' THEN 103
                    ELSE 102
                  END
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
