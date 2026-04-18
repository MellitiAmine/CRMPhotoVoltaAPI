using System;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20260414180000_AddWhatsAppRecommendations")]
public class AddWhatsAppRecommendations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WhatsAppRecommendations",
            schema: "app",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                PhoneNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                ActionType = table.Column<int>(type: "integer", nullable: false),
                Sd = table.Column<double>(type: "double precision", nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                Temperature = table.Column<int>(type: "integer", nullable: false),
                IsSent = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SocietyId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WhatsAppRecommendations", x => x.Id);
                table.ForeignKey(
                    name: "FK_WhatsAppRecommendations_Leads_LeadId",
                    column: x => x.LeadId,
                    principalSchema: "app",
                    principalTable: "Leads",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_WhatsAppRecommendations_LeadId",
            schema: "app",
            table: "WhatsAppRecommendations",
            column: "LeadId");

        migrationBuilder.CreateIndex(
            name: "IX_WhatsAppRecommendations_SocietyId",
            schema: "app",
            table: "WhatsAppRecommendations",
            column: "SocietyId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "WhatsAppRecommendations",
            schema: "app");
    }
}
