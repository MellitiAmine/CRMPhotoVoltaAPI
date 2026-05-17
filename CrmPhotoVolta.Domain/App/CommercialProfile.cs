namespace CrmPhotoVolta.Domain.App;

/// <summary>
/// HR profile and performance metadata for a commercial (sales) user.
/// One record per tenant user who holds the "Commercial" role.
/// The entity is soft-deleted when the user leaves the company.
/// </summary>
public class CommercialProfile : SocietyScopedEntity
{
    /// <summary>Reference to the tenant user account (core.Users table).</summary>
    public Guid UserId { get; set; }

    // ── Personal info ────────────────────────────────────────────────────
    public string FirstName    { get; set; } = string.Empty;
    public string LastName     { get; set; } = string.Empty;
    public string Email        { get; set; } = string.Empty;
    public string? Phone       { get; set; }
    public string? AvatarUrl   { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Address     { get; set; }
    public string? City        { get; set; }

    // ── Emergency contact (stored as flat columns for simplicity) ────────
    public string? EmergencyContactName     { get; set; }
    public string? EmergencyContactPhone    { get; set; }
    public string? EmergencyContactRelation { get; set; }

    // ── Employment details ───────────────────────────────────────────────
    public string  EmployeeId    { get; set; } = string.Empty; // e.g. EMP-2024-001
    public string  Department    { get; set; } = string.Empty;
    public string  Position      { get; set; } = string.Empty;
    public string  ContractType  { get; set; } = CommercialContractTypes.CDI;
    public string  WorkTime      { get; set; } = CommercialWorkTime.FullTime;
    public decimal Salary        { get; set; }
    public string  Status        { get; set; } = CommercialStatuses.Active;
    public string  StartDate     { get; set; } = string.Empty;

    // ── Monthly targets ──────────────────────────────────────────────────
    public decimal MonthlyTarget { get; set; }

    // ── Performance score snapshot (recalculated periodically) ───────────
    public int     ScoreTotal       { get; set; }
    public string  ScoreTier        { get; set; } = CommercialScoreTiers.Low;
    public string  ScoreTrend       { get; set; } = "stable"; // up | stable | down
    public int     ScoreTrendValue  { get; set; }             // Δ points vs last period
    public DateTimeOffset? ScoredAt { get; set; }

    // ── Score breakdown (0 = not yet computed) ───────────────────────────
    public double ScoreActivities { get; set; }
    public double ScoreMeetings   { get; set; }
    public double ScoreLeads      { get; set; }
    public double ScoreDeals      { get; set; }
    public double ScoreAttendance { get; set; }
    public double ScorePenalties  { get; set; }

    // ── KPI snapshot (updated in batch or on-demand) ─────────────────────
    public int     KpiActivitiesCreated    { get; set; }
    public int     KpiMeetingsParticipated { get; set; }
    public int     KpiLeadsAssigned        { get; set; }
    public int     KpiDealsWon             { get; set; }
    public int     KpiQuotesGenerated      { get; set; }
    public decimal KpiRevenueGenerated     { get; set; }
    public double  KpiConversionRate       { get; set; }
    public int     KpiPenalties            { get; set; }

    // ── Attendance snapshot (current month) ──────────────────────────────
    public int    AttendancePresentDays      { get; set; }
    public int    AttendanceTotalWorkingDays { get; set; } = 22;
    public int    AttendanceAbsentDays       { get; set; }
    public int    AttendanceLateDays         { get; set; }
    public double AttendanceHoursWorked      { get; set; }
    public double AttendanceExpectedHours    { get; set; } = 160;
    public double AttendancePct              { get; set; }
}

public static class CommercialStatuses
{
    public const string Active     = "active";
    public const string OnLeave    = "on_leave";
    public const string Suspended  = "suspended";
    public const string Terminated = "terminated";
}

public static class CommercialContractTypes
{
    public const string CDI        = "CDI";
    public const string CDD        = "CDD";
    public const string Stage      = "Stage";
    public const string Freelance  = "Freelance";
    public const string Alternance = "Alternance";
}

public static class CommercialWorkTime
{
    public const string FullTime = "full_time";
    public const string PartTime = "part_time";
}

public static class CommercialScoreTiers
{
    public const string Top     = "top";
    public const string Good    = "good";
    public const string Average = "average";
    public const string Low     = "low";
}
