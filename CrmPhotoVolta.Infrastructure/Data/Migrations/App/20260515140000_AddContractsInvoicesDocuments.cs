using System;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App;

[DbContext(typeof(AppDbContext))]
[Migration("20260515140000_AddContractsInvoicesDocuments")]
public partial class AddContractsInvoicesDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Contracts ────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Contracts",
            schema: "app",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                Reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                SignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                TotalAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                PdfUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SocietyId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Contracts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Contracts_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalSchema: "app",
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Contracts_Clients_ClientId",
                    column: x => x.ClientId,
                    principalSchema: "app",
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Contracts_SocietyId",
            schema: "app",
            table: "Contracts",
            column: "SocietyId");

        migrationBuilder.CreateIndex(
            name: "IX_Contracts_ProjectId",
            schema: "app",
            table: "Contracts",
            column: "ProjectId");

        // ── Invoices ─────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Invoices",
            schema: "app",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                Reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                TotalHt = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                TotalTva = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                TotalTtc = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                PaidAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                PdfUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SocietyId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Invoices", x => x.Id);
                table.ForeignKey(
                    name: "FK_Invoices_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalSchema: "app",
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Invoices_Clients_ClientId",
                    column: x => x.ClientId,
                    principalSchema: "app",
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_SocietyId",
            schema: "app",
            table: "Invoices",
            column: "SocietyId");

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_ProjectId",
            schema: "app",
            table: "Invoices",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_SocietyId_Reference",
            schema: "app",
            table: "Invoices",
            columns: new[] { "SocietyId", "Reference" },
            unique: true);

        // ── InvoiceItems ─────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "InvoiceItems",
            schema: "app",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                UnitPrice = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                TvaRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                TotalHt = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SocietyId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_InvoiceItems_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalSchema: "app",
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_InvoiceItems_Items_ItemId",
                    column: x => x.ItemId,
                    principalSchema: "app",
                    principalTable: "Items",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_InvoiceItems_SocietyId",
            schema: "app",
            table: "InvoiceItems",
            column: "SocietyId");

        migrationBuilder.CreateIndex(
            name: "IX_InvoiceItems_InvoiceId",
            schema: "app",
            table: "InvoiceItems",
            column: "InvoiceId");

        // ── Payments ──────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Payments",
            schema: "app",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                PaidOn = table.Column<DateOnly>(type: "date", nullable: false),
                Method = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SocietyId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Payments_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalSchema: "app",
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Payments_SocietyId",
            schema: "app",
            table: "Payments",
            column: "SocietyId");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_InvoiceId",
            schema: "app",
            table: "Payments",
            column: "InvoiceId");

        // ── ProjectDocuments ──────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "ProjectDocuments",
            schema: "app",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SocietyId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProjectDocuments", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProjectDocuments_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalSchema: "app",
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProjectDocuments_SocietyId",
            schema: "app",
            table: "ProjectDocuments",
            column: "SocietyId");

        migrationBuilder.CreateIndex(
            name: "IX_ProjectDocuments_ProjectId",
            schema: "app",
            table: "ProjectDocuments",
            column: "ProjectId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProjectDocuments", schema: "app");
        migrationBuilder.DropTable(name: "Payments", schema: "app");
        migrationBuilder.DropTable(name: "InvoiceItems", schema: "app");
        migrationBuilder.DropTable(name: "Invoices", schema: "app");
        migrationBuilder.DropTable(name: "Contracts", schema: "app");
    }
}
