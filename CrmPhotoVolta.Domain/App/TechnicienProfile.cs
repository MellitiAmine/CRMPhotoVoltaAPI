namespace CrmPhotoVolta.Domain.App;

/// <summary>
/// HR profile and performance metadata for a technicien (field technician) user.
/// One record per tenant user who holds the "Technicien" role.
/// The entity is soft-deleted when the user leaves the company.
/// </summary>
public class TechnicienProfile : SocietyScopedEntity
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

    // ── Emergency contact ────────────────────────────────────────────────
    public string? EmergencyContactName     { get; set; }
    public string? EmergencyContactPhone    { get; set; }
    public string? EmergencyContactRelation { get; set; }

    // ── Employment details ───────────────────────────────────────────────
    public string  EmployeeId    { get; set; } = string.Empty;
    public string  Department    { get; set; } = string.Empty;
    public string  Position      { get; set; } = string.Empty;
    public string  ContractType  { get; set; } = TechnicienContractTypes.CDI;
    public string  WorkTime      { get; set; } = TechnicienWorkTime.FullTime;
    public decimal Salary        { get; set; }
    public string  Status        { get; set; } = TechnicienStatuses.Active;
    public string  StartDate     { get; set; } = string.Empty;

    /// <summary>Monthly installation / intervention target count.</summary>
    public int MonthlyTarget { get; set; }

    // ── Performance score snapshot ───────────────────────────────────────
    public int     ScoreTotal       { get; set; }
    public string  ScoreTier        { get; set; } = TechnicienScoreTiers.Low;
    public string  ScoreTrend       { get; set; } = "stable";
    public int     ScoreTrendValue  { get; set; }
    public DateTimeOffset? ScoredAt { get; set; }

    public double ScoreInterventions  { get; set; }
    public double ScoreSiteVisits     { get; set; }
    public double ScoreProjects       { get; set; }
    public double ScoreInstallations  { get; set; }
    public double ScoreAttendance     { get; set; }
    public double ScorePenalties      { get; set; }

    // ── KPI snapshot ─────────────────────────────────────────────────────
    public int     KpiInterventionsCompleted { get; set; }
    public int     KpiSiteVisitsCompleted    { get; set; }
    public int     KpiProjectsAssigned       { get; set; }
    public int     KpiInstallationsCompleted { get; set; }
    public int     KpiReportsSubmitted       { get; set; }
    public double  KpiHoursOnSite            { get; set; }
    public double  KpiOnTimeRate             { get; set; }
    public int     KpiPenalties              { get; set; }

    // ── Attendance snapshot (current month) ──────────────────────────────
    public int    AttendancePresentDays      { get; set; }
    public int    AttendanceTotalWorkingDays { get; set; } = 22;
    public int    AttendanceAbsentDays       { get; set; }
    public int    AttendanceLateDays         { get; set; }
    public double AttendanceHoursWorked      { get; set; }
    public double AttendanceExpectedHours    { get; set; } = 160;
    public double AttendancePct              { get; set; }
}

public static class TechnicienStatuses
{
    public const string Active     = "active";
    public const string OnLeave    = "on_leave";
    public const string Suspended  = "suspended";
    public const string Terminated = "terminated";
}

public static class TechnicienContractTypes
{
    public const string CDI        = "CDI";
    public const string CDD        = "CDD";
    public const string Stage      = "Stage";
    public const string Freelance  = "Freelance";
    public const string Alternance = "Alternance";
}

public static class TechnicienWorkTime
{
    public const string FullTime = "full_time";
    public const string PartTime = "part_time";
}

public static class TechnicienScoreTiers
{
    public const string Top     = "top";
    public const string Good    = "good";
    public const string Average = "average";
    public const string Low     = "low";
}
