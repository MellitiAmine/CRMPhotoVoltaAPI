using System.ComponentModel.DataAnnotations;

namespace CrmPhotoVolta.Application.Crm.Techniciens;

// ══════════════════════════════════════════════════════════════
// Response DTOs
// ══════════════════════════════════════════════════════════════

public sealed record TechnicienScoreBreakdownDto(
    double Interventions,
    double SiteVisits,
    double Projects,
    double Installations,
    double Attendance,
    double Penalties
);

public sealed record TechnicienScoreDto(
    int    Total,
    string Tier,
    string Trend,
    int    TrendValue,
    TechnicienScoreBreakdownDto Breakdown
);

public sealed record TechnicienKpisDto(
    int     InterventionsCompleted,
    int     SiteVisitsCompleted,
    int     ProjectsAssigned,
    int     InstallationsCompleted,
    int     ReportsSubmitted,
    double  HoursOnSite,
    double  OnTimeRate,
    int     Penalties
);

public sealed record TechnicienAttendanceDto(
    int    PresentDays,
    int    TotalWorkingDays,
    int    AbsentDays,
    int    LateDays,
    double HoursWorked,
    double ExpectedHours,
    double AttendancePct
);

public sealed record TechnicienEmergencyContactDto(
    string? Name,
    string? Phone,
    string? Relation
);

public sealed record TechnicienProfileDto(
    Guid   Id,
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
    int     MonthlyTarget,
    TechnicienScoreDto       Score,
    TechnicienKpisDto        Kpis,
    TechnicienAttendanceDto  Attendance,
    TechnicienEmergencyContactDto EmergencyContact,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public sealed record TechnicienListItemDto(
    Guid Id,
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
    int     MonthlyTarget,
    TechnicienScoreDto  Score,
    TechnicienKpisDto   Kpis,
    TechnicienAttendanceDto Attendance
);

public sealed record TechnicienStatsDto(
    int     TotalCount,
    int     ActiveCount,
    int     OnLeaveCount,
    int     TopPerformers,
    int     LowPerformers,
    decimal TotalSalary,
    double  AvgScore,
    double  AvgAttendancePct,
    double  AvgOnTimeRate,
    double  TotalHoursOnSite
);

// ══════════════════════════════════════════════════════════════
// Request DTOs
// ══════════════════════════════════════════════════════════════

public sealed record CreateTechnicienRequest(
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
    int MonthlyTarget = 0,
    string? EmergencyContactName     = null,
    string? EmergencyContactPhone    = null,
    string? EmergencyContactRelation = null
);

public sealed record UpdateTechnicienRequest(
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
    int? MonthlyTarget,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelation
);

public sealed record UpdateTechnicienKpisRequest(
    int     InterventionsCompleted,
    int     SiteVisitsCompleted,
    int     ProjectsAssigned,
    int     InstallationsCompleted,
    int     ReportsSubmitted,
    double  HoursOnSite,
    double  OnTimeRate,
    int     Penalties
);

public sealed record TechnicienListQuery(
    string? Search          = null,
    string? Status          = null,
    string? ContractType    = null,
    string? Department      = null,
    string? ScoreTier       = null,
    int     Page            = 1,
    int     PageSize        = 20
);
