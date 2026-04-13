using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class HardenTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SocietyId",
                schema: "app",
                table: "ProjectStageTracking",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SocietyId",
                schema: "app",
                table: "InstallationPhotos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SocietyId",
                schema: "app",
                table: "InstallationChecklist",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE app."ProjectStageTracking" t
                SET "SocietyId" = p."SocietyId"
                FROM app."Projects" p
                WHERE t."ProjectId" = p."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE app."InstallationPhotos" p
                SET "SocietyId" = i."SocietyId"
                FROM app."Installations" i
                WHERE p."InstallationId" = i."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE app."InstallationChecklist" c
                SET "SocietyId" = i."SocietyId"
                FROM app."Installations" i
                WHERE c."InstallationId" = i."Id";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SocietyId",
                schema: "app",
                table: "ProjectStageTracking",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SocietyId",
                schema: "app",
                table: "InstallationPhotos",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SocietyId",
                schema: "app",
                table: "InstallationChecklist",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStageTracking_SocietyId",
                schema: "app",
                table: "ProjectStageTracking",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_InstallationPhotos_SocietyId",
                schema: "app",
                table: "InstallationPhotos",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_InstallationChecklist_SocietyId",
                schema: "app",
                table: "InstallationChecklist",
                column: "SocietyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectStageTracking_SocietyId",
                schema: "app",
                table: "ProjectStageTracking");

            migrationBuilder.DropIndex(
                name: "IX_InstallationPhotos_SocietyId",
                schema: "app",
                table: "InstallationPhotos");

            migrationBuilder.DropIndex(
                name: "IX_InstallationChecklist_SocietyId",
                schema: "app",
                table: "InstallationChecklist");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                schema: "app",
                table: "ProjectStageTracking");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                schema: "app",
                table: "InstallationPhotos");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                schema: "app",
                table: "InstallationChecklist");
        }
    }
}
