using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.Platform
{
    /// <inheritdoc />
    public partial class InitialPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.CreateTable(
                name: "PlatformPermissions",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformRoles",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformUsers",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformRolePermissions",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformPermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformRolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformRolePermissions_PlatformPermissions_PlatformPermiss~",
                        column: x => x.PlatformPermissionId,
                        principalSchema: "platform",
                        principalTable: "PlatformPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlatformRolePermissions_PlatformRoles_PlatformRoleId",
                        column: x => x.PlatformRoleId,
                        principalSchema: "platform",
                        principalTable: "PlatformRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlatformUserRoles",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformUserRoles_PlatformRoles_PlatformRoleId",
                        column: x => x.PlatformRoleId,
                        principalSchema: "platform",
                        principalTable: "PlatformRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlatformUserRoles_PlatformUsers_PlatformUserId",
                        column: x => x.PlatformUserId,
                        principalSchema: "platform",
                        principalTable: "PlatformUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformPermissions_Code",
                schema: "platform",
                table: "PlatformPermissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRolePermissions_PlatformPermissionId",
                schema: "platform",
                table: "PlatformRolePermissions",
                column: "PlatformPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRolePermissions_PlatformRoleId_PlatformPermissionId",
                schema: "platform",
                table: "PlatformRolePermissions",
                columns: new[] { "PlatformRoleId", "PlatformPermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_Name",
                schema: "platform",
                table: "PlatformRoles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUserRoles_PlatformRoleId",
                schema: "platform",
                table: "PlatformUserRoles",
                column: "PlatformRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUserRoles_PlatformUserId_PlatformRoleId",
                schema: "platform",
                table: "PlatformUserRoles",
                columns: new[] { "PlatformUserId", "PlatformRoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUsers_Email",
                schema: "platform",
                table: "PlatformUsers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformRolePermissions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PlatformUserRoles",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PlatformPermissions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PlatformRoles",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PlatformUsers",
                schema: "platform");
        }
    }
}
