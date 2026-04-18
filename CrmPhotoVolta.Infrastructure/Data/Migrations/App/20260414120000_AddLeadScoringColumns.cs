using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class AddLeadScoringColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                schema: "app",
                table: "Leads",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "BonusBudgetConfirmed",
                schema: "app",
                table: "Leads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BonusDecisionMaker",
                schema: "app",
                table: "Leads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BonusFinancingInterest",
                schema: "app",
                table: "Leads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BonusQuoteRequested",
                schema: "app",
                table: "Leads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "EstimatedKw",
                schema: "app",
                table: "Leads",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Lvi",
                schema: "app",
                table: "Leads",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MonthlyBillEur",
                schema: "app",
                table: "Leads",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ScoreBreakdownActivity",
                schema: "app",
                table: "Leads",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ScoreBreakdownIntention",
                schema: "app",
                table: "Leads",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ScoreBreakdownInteraction",
                schema: "app",
                table: "Leads",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ScoreBreakdownPenalties",
                schema: "app",
                table: "Leads",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ScoreBreakdownPotential",
                schema: "app",
                table: "Leads",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ScoreBreakdownSatisfaction",
                schema: "app",
                table: "Leads",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Sd",
                schema: "app",
                table: "Leads",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScoredAt",
                schema: "app",
                table: "Leads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_SocietyId_Lvi",
                schema: "app",
                table: "Leads",
                columns: new[] { "SocietyId", "Lvi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leads_SocietyId_Lvi",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ScoredAt",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Sd",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ScoreBreakdownSatisfaction",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ScoreBreakdownPotential",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ScoreBreakdownPenalties",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ScoreBreakdownInteraction",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ScoreBreakdownIntention",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ScoreBreakdownActivity",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "MonthlyBillEur",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Lvi",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "EstimatedKw",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "BonusQuoteRequested",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "BonusFinancingInterest",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "BonusDecisionMaker",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "BonusBudgetConfirmed",
                schema: "app",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                schema: "app",
                table: "Leads");
        }
    }
}
