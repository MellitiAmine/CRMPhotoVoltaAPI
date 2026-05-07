using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class EventsLeadId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LeadId",
                schema: "app",
                table: "Events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_LeadId",
                schema: "app",
                table: "Events",
                column: "LeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Leads_LeadId",
                schema: "app",
                table: "Events",
                column: "LeadId",
                principalSchema: "app",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Leads_LeadId",
                schema: "app",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_LeadId",
                schema: "app",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LeadId",
                schema: "app",
                table: "Events");
        }
    }
}
