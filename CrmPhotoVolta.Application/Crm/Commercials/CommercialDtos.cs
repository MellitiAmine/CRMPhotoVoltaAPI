using System.ComponentModel.DataAnnotations;

namespace CrmPhotoVolta.Application.Crm.Commercials;

// ══════════════════════════════════════════════════════════════
// Response DTOs
// ══════════════════════════════════════════════════════════════

public sealed record CommercialScoreBreakdownDto(
    double Activities,
    double Meetings,
    double Leads,
    double Deals,
    double Attendance,
    double Penalties
);

public sealed record CommercialScoreDto(
    int    Total,
    string Tier,
    string Trend,
    int    TrendValue,
    CommercialScoreBreakdownDto Breakdown
);

public sealed record CommercialKpisDto(
    int     ActivitiesCreated,
    int     MeetingsParticipated,
    int     LeadsAssigned,
    int     DealsWon,
    int     QuotesGenerated,
    decimal RevenueGenerated,
    double  ConversionRate,
    int     Penalties
);

public sealed record CommercialAttendanceDto(
    int    PresentDays,
    int    TotalWorkingDays,
    int    AbsentDays,
    int    LateDays,
    double HoursWorked,
    double ExpectedHours,
    double AttendancePct
);

public sealed record CommercialEmergencyContactDto(
    string? Name,
    string? Phone,
    string? Relation
);

public sealed record CommercialProfileDto(
    Guid   Id,
    /// <summary>Core user id when this profile is linked to a real society member; otherwise null (e.g. placeholder id only in DB).</summary>
    Guid?  UserId,
    string EmployeeId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? AvatarUrl,
    string? DateOfBirth,
    string? Address,
    string? City,
    string Department,
    string Position,
    string ContractType,
    string WorkTime,
    decimal Salary,
    string  Status,
    string  StartDate,
    decimal MonthlyTarget,
    CommercialScoreDto       Score,
    CommercialKpisDto        Kpis,
    CommercialAttendanceDto  Attendance,
    CommercialEmergencyContactDto EmergencyContact,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public sealed record CommercialListItemDto(
    Guid Id,
    /// <summary>Non-null only when this profile is linked to an active Core user in the same society (safe for calendar invites).</summary>
    Guid? UserId,
    string EmployeeId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Department,
    string Position,
    string ContractType,
    string WorkTime,
    string Status,
    string StartDate,
    decimal Salary,
    decimal MonthlyTarget,
    CommercialScoreDto  Score,
    CommercialKpisDto   Kpis,
    CommercialAttendanceDto Attendance
);

public sealed record CommercialStatsDto(
    int     TotalCount,
    int     ActiveCount,
    int     OnLeaveCount,
    int     TopPerformers,
    int     LowPerformers,
    decimal TotalSalary,
    double  AvgScore,
    double  AvgAttendancePct,
    double  AvgConversionRate,
    decimal TotalRevenue
);

// ══════════════════════════════════════════════════════════════
// Request DTOs
// ══════════════════════════════════════════════════════════════

public sealed record CreateCommercialRequest(
    Guid? UserId,
    string? EmployeeId,
    [Required] string FirstName,
    [Required] string LastName,
    [Required] string Email,
    string? Phone,
    string? DateOfBirth,
    string? Address,
    string? City,
    [Required] string Department,
    [Required] string Position,
    [Required] string ContractType,
    string WorkTime = "full_time",
    decimal Salary = 0,
    [Required] string StartDate = "",
    decimal MonthlyTarget = 0,
    string? EmergencyContactName     = null,
    string? EmergencyContactPhone    = null,
    string? EmergencyContactRelation = null
);

public sealed record UpdateCommercialRequest(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? DateOfBirth,
    string? Address,
    string? City,
    string? Department,
    string? Position,
    string? ContractType,
    string? WorkTime,
    decimal? Salary,
    string? Status,
    decimal? MonthlyTarget,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelation
);

public sealed record UpdateCommercialKpisRequest(
    int     ActivitiesCreated,
    int     MeetingsParticipated,
    int     LeadsAssigned,
    int     DealsWon,
    int     QuotesGenerated,
    decimal RevenueGenerated,
    double  ConversionRate,
    int     Penalties
);

public sealed record CommercialListQuery(
    string? Search          = null,
    string? Status          = null,
    string? ContractType    = null,
    string? Department      = null,
    string? ScoreTier       = null,
    int     Page            = 1,
    int     PageSize        = 20
);
