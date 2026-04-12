using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.Core;

/// <inheritdoc />
[DbContext(typeof(CoreDbContext))]
[Migration("20260412154500_SubscriptionPlansAndPlatformAdmin")]
public class SubscriptionPlansAndPlatformAdmin : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "BillingPeriodMonths",
            schema: "core",
            table: "SubscriptionPlans",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "Code",
            schema: "core",
            table: "SubscriptionPlans",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Currency",
            schema: "core",
            table: "SubscriptionPlans",
            type: "character varying(8)",
            maxLength: 8,
            nullable: false,
            defaultValue: "TND");

        migrationBuilder.AddColumn<int>(
            name: "TrialDurationMonths",
            schema: "core",
            table: "SubscriptionPlans",
            type: "integer",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE core."SubscriptionPlans"
            SET "Code" = 'FREE_TRIAL_3M',
                "Currency" = 'TND',
                "TrialDurationMonths" = 3,
                "BillingPeriodMonths" = 3,
                "Price" = 0
            WHERE "Name" = 'Starter';
            """);

        migrationBuilder.CreateIndex(
            name: "IX_SubscriptionPlans_Code",
            schema: "core",
            table: "SubscriptionPlans",
            column: "Code",
            unique: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsPlatformAdministrator",
            schema: "core",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsPlatformAdministrator",
            schema: "core",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_SubscriptionPlans_Code",
            schema: "core",
            table: "SubscriptionPlans");

        migrationBuilder.DropColumn(
            name: "TrialDurationMonths",
            schema: "core",
            table: "SubscriptionPlans");

        migrationBuilder.DropColumn(
            name: "Currency",
            schema: "core",
            table: "SubscriptionPlans");

        migrationBuilder.DropColumn(
            name: "Code",
            schema: "core",
            table: "SubscriptionPlans");

        migrationBuilder.DropColumn(
            name: "BillingPeriodMonths",
            schema: "core",
            table: "SubscriptionPlans");
    }
}
