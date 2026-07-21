namespace CrmPhotoVolta.Application.Crm.Commercials;

public sealed record CommercialTimeEntryDto(
    Guid Id,
    Guid CommercialProfileId,
    string WorkDate,
    string PunchType,
    string Time,
    double TotalHours,
    string? Notes,
    DateTimeOffset CreatedAt
);

public sealed record CreateCommercialTimeEntryRequest(
    string WorkDate,
    string PunchType,
    string Time,
    string? Notes = null
);

public sealed record UpdateCommercialTimeEntryRequest(
    string? WorkDate,
    string? PunchType,
    string? Time,
    string? Notes
);

public sealed record CommercialAttendanceMonthDto(
    int Year,
    int Month,
    int PresentDays,
    int TotalWorkingDays,
    int AbsentDays,
    int LateDays,
    double HoursWorked,
    double ExpectedHours,
    double AttendancePct,
    DateTimeOffset ComputedAt
);

/// <summary>
/// Monthly bundle: summary stats + punch list + list of date strings (yyyy-MM-dd) where first check-in was late.
/// </summary>
public sealed record CommercialTimeMonthBundleDto(
    CommercialAttendanceMonthDto Summary,
    IReadOnlyList<CommercialTimeEntryDto> Entries,
    IReadOnlyList<string> LateDates
);

public sealed record UpdateCommercialAccountRequest(
    string? Email,
    string? NewPassword
);

public sealed record CreateCommercialResultDto(
    CommercialProfileDto Profile,
    string? InitialPassword
);
