using System;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App;

[DbContext(typeof(AppDbContext))]
[Migration("20260415120000_AddItemsAndQuoteTotals")]
public class AddItemsAndQuoteTotals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Items",
            schema: "app",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                DefaultPrice = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                TvaRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SocietyId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Items", x => x.Id); });

        migrationBuilder.CreateIndex(
            name: "IX_Items_SocietyId",
            schema: "app",
            table: "Items",
            column: "SocietyId");

        migrationBuilder.AddColumn<Guid>(
            name: "ItemId",
            schema: "app",
            table: "QuoteItems",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "Discount",
            schema: "app",
            table: "QuoteItems",
            type: "numeric(5,2)",
            precision: 5,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "TvaRate",
            schema: "app",
            table: "QuoteItems",
            type: "numeric(5,2)",
            precision: 5,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "TotalHt",
            schema: "app",
            table: "QuoteItems",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AlterColumn<decimal>(
            name: "Quantity",
            schema: "app",
            table: "QuoteItems",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric");

        migrationBuilder.AlterColumn<decimal>(
            name: "UnitPrice",
            schema: "app",
            table: "QuoteItems",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric");

        migrationBuilder.AddColumn<decimal>(
            name: "TotalHt",
            schema: "app",
            table: "Quotes",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "TotalTva",
            schema: "app",
            table: "Quotes",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "TotalTtc",
            schema: "app",
            table: "Quotes",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "QuoteDate",
            schema: "app",
            table: "Quotes",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "TotalAmount",
            schema: "app",
            table: "Quotes",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric");

        migrationBuilder.CreateIndex(
            name: "IX_QuoteItems_ItemId",
            schema: "app",
            table: "QuoteItems",
            column: "ItemId");

        migrationBuilder.AddForeignKey(
            name: "FK_QuoteItems_Items_ItemId",
            schema: "app",
            table: "QuoteItems",
            column: "ItemId",
            principalSchema: "app",
            principalTable: "Items",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.Sql(
            """
            UPDATE app."QuoteItems" qi
            SET "TotalHt" = qi."Quantity" * qi."UnitPrice"
            WHERE qi."TotalHt" = 0;
            """);

        migrationBuilder.Sql(
            """
            UPDATE app."Quotes" q
            SET "TotalHt" = COALESCE(s.sum_ht, 0),
                "TotalTva" = COALESCE(s.sum_tva, 0),
                "TotalTtc" = COALESCE(s.sum_ht, 0) + COALESCE(s.sum_tva, 0)
            FROM (
                SELECT "QuoteId",
                       SUM("TotalHt") AS sum_ht,
                       SUM("TotalHt" * "TvaRate" / 100.0) AS sum_tva
                FROM app."QuoteItems"
                WHERE NOT "IsDeleted"
                GROUP BY "QuoteId"
            ) s
            WHERE q."Id" = s."QuoteId";
            """);

        migrationBuilder.Sql(
            """
            UPDATE app."Quotes" q
            SET "TotalAmount" = q."TotalTtc"
            WHERE q."TotalTtc" <> 0 OR EXISTS (
                SELECT 1 FROM app."QuoteItems" qi WHERE qi."QuoteId" = q."Id" AND NOT qi."IsDeleted");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_QuoteItems_Items_ItemId",
            schema: "app",
            table: "QuoteItems");

        migrationBuilder.DropIndex(
            name: "IX_QuoteItems_ItemId",
            schema: "app",
            table: "QuoteItems");

        migrationBuilder.DropColumn(
            name: "ItemId",
            schema: "app",
            table: "QuoteItems");

        migrationBuilder.DropColumn(
            name: "Discount",
            schema: "app",
            table: "QuoteItems");

        migrationBuilder.DropColumn(
            name: "TvaRate",
            schema: "app",
            table: "QuoteItems");

        migrationBuilder.DropColumn(
            name: "TotalHt",
            schema: "app",
            table: "QuoteItems");

        migrationBuilder.AlterColumn<decimal>(
            name: "Quantity",
            schema: "app",
            table: "QuoteItems",
            type: "numeric",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,2)",
            oldPrecision: 18,
            oldScale: 2);

        migrationBuilder.AlterColumn<decimal>(
            name: "UnitPrice",
            schema: "app",
            table: "QuoteItems",
            type: "numeric",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,3)",
            oldPrecision: 18,
            oldScale: 3);

        migrationBuilder.DropColumn(
            name: "TotalHt",
            schema: "app",
            table: "Quotes");

        migrationBuilder.DropColumn(
            name: "TotalTva",
            schema: "app",
            table: "Quotes");

        migrationBuilder.DropColumn(
            name: "TotalTtc",
            schema: "app",
            table: "Quotes");

        migrationBuilder.DropColumn(
            name: "QuoteDate",
            schema: "app",
            table: "Quotes");

        migrationBuilder.AlterColumn<decimal>(
            name: "TotalAmount",
            schema: "app",
            table: "Quotes",
            type: "numeric",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,3)",
            oldPrecision: 18,
            oldScale: 3);

        migrationBuilder.DropTable(
            name: "Items",
            schema: "app");
    }
}
