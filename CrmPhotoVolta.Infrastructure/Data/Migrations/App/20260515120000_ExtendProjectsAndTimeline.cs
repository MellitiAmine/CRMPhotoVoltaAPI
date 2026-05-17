using System;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App;

[DbContext(typeof(AppDbContext))]
[Migration("20260515120000_ExtendProjectsAndTimeline")]
public partial class ExtendProjectsAndTimeline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "LeadId",
            schema: "app",
            table: "Projects",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "QuoteId",
            schema: "app",
            table: "Projects",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Reference",
            schema: "app",
            table: "Projects",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Priority",
            schema: "app",
            table: "Projects",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "Notes",
            schema: "app",
            table: "Projects",
            type: "character varying(4000)",
            maxLength: 4000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RoofType",
            schema: "app",
            table: "Projects",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "InstallationType",
            schema: "app",
            table: "Projects",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "PanelCount",
            schema: "app",
            table: "Projects",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "InverterCount",
            schema: "app",
            table: "Projects",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "TotalHt",
            schema: "app",
            table: "Projects",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "TotalTva",
            schema: "app",
            table: "Projects",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "TotalTtc",
            schema: "app",
            table: "Projects",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "EstimatedMargin",
            schema: "app",
            table: "Projects",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: true);

        migrationBuilder.AddColumn<DateOnly>(
            name: "ExpectedInstallationDate",
            schema: "app",
            table: "Projects",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "CommercialUserId",
            schema: "app",
            table: "Projects",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LastActivityAt",
            schema: "app",
            table: "Projects",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Description",
            schema: "app",
            table: "Tasks",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Priority",
            schema: "app",
            table: "Tasks",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CompletedAt",
            schema: "app",
            table: "Tasks",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "ProjectTimelineEvents",
            schema: "app",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SocietyId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProjectTimelineEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProjectTimelineEvents_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalSchema: "app",
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Projects_LeadId",
            schema: "app",
            table: "Projects",
            column: "LeadId");

        migrationBuilder.CreateIndex(
            name: "IX_Projects_QuoteId",
            schema: "app",
            table: "Projects",
            column: "QuoteId");

        migrationBuilder.CreateIndex(
            name: "IX_Projects_SocietyId_LeadId",
            schema: "app",
            table: "Projects",
            columns: new[] { "SocietyId", "LeadId" },
            unique: true,
            filter: "\"LeadId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_ProjectTimelineEvents_ProjectId",
            schema: "app",
            table: "ProjectTimelineEvents",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_ProjectTimelineEvents_SocietyId",
            schema: "app",
            table: "ProjectTimelineEvents",
            column: "SocietyId");

        migrationBuilder.AddForeignKey(
            name: "FK_Projects_Leads_LeadId",
            schema: "app",
            table: "Projects",
            column: "LeadId",
            principalSchema: "app",
            principalTable: "Leads",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_Projects_Quotes_QuoteId",
            schema: "app",
            table: "Projects",
            column: "QuoteId",
            principalSchema: "app",
            principalTable: "Quotes",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.Sql(
            """
            UPDATE app."Projects" SET "Status" = 'New' WHERE "Status" = 'Planned';
            UPDATE app."Projects" SET "Status" = 'Installation' WHERE "Status" = 'InProgress';
            UPDATE app."Projects" SET "Status" = 'Completed' WHERE "Status" = 'Done';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Projects_Leads_LeadId",
            schema: "app",
            table: "Projects");

        migrationBuilder.DropForeignKey(
            name: "FK_Projects_Quotes_QuoteId",
            schema: "app",
            table: "Projects");

        migrationBuilder.DropTable(
            name: "ProjectTimelineEvents",
            schema: "app");

        migrationBuilder.DropIndex(
            name: "IX_Projects_SocietyId_LeadId",
            schema: "app",
            table: "Projects");

        migrationBuilder.DropIndex(
            name: "IX_Projects_QuoteId",
            schema: "app",
            table: "Projects");

        migrationBuilder.DropIndex(
            name: "IX_Projects_LeadId",
            schema: "app",
            table: "Projects");

        migrationBuilder.DropColumn(name: "LeadId", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "QuoteId", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "Reference", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "Priority", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "Notes", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "RoofType", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "InstallationType", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "PanelCount", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "InverterCount", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "TotalHt", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "TotalTva", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "TotalTtc", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "EstimatedMargin", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "ExpectedInstallationDate", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "CommercialUserId", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "LastActivityAt", schema: "app", table: "Projects");
        migrationBuilder.DropColumn(name: "Description", schema: "app", table: "Tasks");
        migrationBuilder.DropColumn(name: "Priority", schema: "app", table: "Tasks");
        migrationBuilder.DropColumn(name: "CompletedAt", schema: "app", table: "Tasks");
    }
}
