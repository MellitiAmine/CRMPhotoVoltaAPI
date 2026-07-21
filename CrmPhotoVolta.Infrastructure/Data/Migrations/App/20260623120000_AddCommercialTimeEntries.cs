using System;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260623120000_AddCommercialTimeEntries")]
    public partial class AddCommercialTimeEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommercialTimeEntries",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommercialProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PunchType = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialTimeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommercialTimeEntries_CommercialProfiles_CommercialProfileId",
                        column: x => x.CommercialProfileId,
                        principalSchema: "app",
                        principalTable: "CommercialProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommercialTimeEntries_SocietyId_Profile_WorkDate",
                schema: "app",
                table: "CommercialTimeEntries",
                columns: new[] { "SocietyId", "CommercialProfileId", "WorkDate" });

            migrationBuilder.CreateTable(
                name: "CommercialAttendanceMonths",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommercialProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    PresentDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalWorkingDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AbsentDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LateDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    HoursWorked = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    ExpectedHours = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    AttendancePct = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialAttendanceMonths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommercialAttendanceMonths_CommercialProfiles_CommercialProfileId",
                        column: x => x.CommercialProfileId,
                        principalSchema: "app",
                        principalTable: "CommercialProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommercialAttendanceMonths_SocietyId_Profile_Year_Month",
                schema: "app",
                table: "CommercialAttendanceMonths",
                columns: new[] { "SocietyId", "CommercialProfileId", "Year", "Month" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CommercialTimeEntries", schema: "app");
            migrationBuilder.DropTable(name: "CommercialAttendanceMonths", schema: "app");
        }
    }
}
