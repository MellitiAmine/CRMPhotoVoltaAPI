using System;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App;

/// <summary>Append-only lead audit journal + backfill from legacy system LeadActivities (types 100–103).</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260509150000_LeadJournalEntries")]
public partial class LeadJournalEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LeadJournalEntries",
            schema: "app",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                RelatedEntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                MetadataJson = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LeadJournalEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_LeadJournalEntries_Leads_LeadId",
                    column: x => x.LeadId,
                    principalSchema: "app",
                    principalTable: "Leads",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LeadJournalEntries_SocietyId",
            schema: "app",
            table: "LeadJournalEntries",
            column: "SocietyId");

        migrationBuilder.CreateIndex(
            name: "IX_LeadJournalEntries_LeadId_CreatedAt",
            schema: "app",
            table: "LeadJournalEntries",
            columns: new[] { "LeadId", "CreatedAt" });

        migrationBuilder.Sql(
            """
            INSERT INTO app."LeadJournalEntries" (
                "Id", "SocietyId", "LeadId", "Action", "RelatedEntityType", "RelatedEntityId",
                "MetadataJson", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
            SELECT gen_random_uuid(),
                   a."SocietyId",
                   a."LeadId",
                   CASE a."Type"
                     WHEN 100 THEN 'commercial.assigned'
                     WHEN 101 THEN 'lead.status_changed'
                     WHEN 103 THEN 'lead.converted'
                     ELSE 'legacy.activity'
                   END,
                   'lead_activity',
                   a."Id",
                   jsonb_build_object('legacyNotes', a."Notes", 'legacyActivityType', a."Type")::text,
                   a."CreatedAt",
                   a."CreatedByUserId",
                   NULL,
                   NULL,
                   FALSE
            FROM app."LeadActivities" a
            WHERE a."IsDeleted" = FALSE
              AND a."Type" IN (100, 101, 103);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LeadJournalEntries", schema: "app");
    }
}
