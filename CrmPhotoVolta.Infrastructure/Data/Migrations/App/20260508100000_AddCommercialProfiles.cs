using System;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260508100000_AddCommercialProfiles")]
    /// <inheritdoc />
    public partial class AddCommercialProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommercialProfiles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    // Personal
                    FirstName  = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName   = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email      = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone      = table.Column<string>(type: "character varying(30)",  maxLength: 30,  nullable: true),
                    AvatarUrl  = table.Column<string>(type: "text", nullable: true),
                    DateOfBirth = table.Column<string>(type: "text", nullable: true),
                    Address    = table.Column<string>(type: "text", nullable: true),
                    City       = table.Column<string>(type: "text", nullable: true),
                    // Emergency contact
                    EmergencyContactName     = table.Column<string>(type: "text", nullable: true),
                    EmergencyContactPhone    = table.Column<string>(type: "text", nullable: true),
                    EmergencyContactRelation = table.Column<string>(type: "text", nullable: true),
                    // Employment
                    EmployeeId    = table.Column<string>(type: "character varying(50)",  maxLength: 50,  nullable: false),
                    Department    = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Position      = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ContractType  = table.Column<string>(type: "character varying(30)",  maxLength: 30,  nullable: false, defaultValue: "CDI"),
                    WorkTime      = table.Column<string>(type: "character varying(20)",  maxLength: 20,  nullable: false, defaultValue: "full_time"),
                    Salary        = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Status        = table.Column<string>(type: "character varying(20)",  maxLength: 20,  nullable: false, defaultValue: "active"),
                    StartDate     = table.Column<string>(type: "character varying(10)",  maxLength: 10,  nullable: false, defaultValue: ""),
                    MonthlyTarget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    // Score
                    ScoreTotal      = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ScoreTier       = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "average"),
                    ScoreTrend      = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "stable"),
                    ScoreTrendValue = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ScoredAt        = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    // Score breakdown
                    ScoreActivities = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    ScoreMeetings   = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    ScoreLeads      = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    ScoreDeals      = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    ScoreAttendance = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    ScorePenalties  = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    // KPIs
                    KpiActivitiesCreated    = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    KpiMeetingsParticipated = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    KpiLeadsAssigned        = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    KpiDealsWon             = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    KpiQuotesGenerated      = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    KpiRevenueGenerated     = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    KpiConversionRate       = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    KpiPenalties            = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    // Attendance snapshot
                    AttendancePresentDays      = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AttendanceTotalWorkingDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 22),
                    AttendanceAbsentDays       = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AttendanceLateDays         = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AttendanceHoursWorked      = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    AttendanceExpectedHours    = table.Column<double>(type: "double precision", nullable: false, defaultValue: 160.0),
                    AttendancePct              = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    // Audit
                    CreatedAt   = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt   = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted   = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommercialProfiles_SocietyId",
                schema: "app",
                table: "CommercialProfiles",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialProfiles_SocietyId_UserId",
                schema: "app",
                table: "CommercialProfiles",
                columns: new[] { "SocietyId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialProfiles_SocietyId_EmployeeId",
                schema: "app",
                table: "CommercialProfiles",
                columns: new[] { "SocietyId", "EmployeeId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommercialProfiles",
                schema: "app");
        }
    }
}
