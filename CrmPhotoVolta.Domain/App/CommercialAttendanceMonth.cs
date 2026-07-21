namespace CrmPhotoVolta.Domain.App;

/// <summary>Aggregated attendance for one commercial profile and calendar month.</summary>
public class CommercialAttendanceMonth : SocietyScopedEntity
{
    public Guid CommercialProfileId { get; set; }
    public CommercialProfile? CommercialProfile { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public int PresentDays { get; set; }
    public int TotalWorkingDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public double HoursWorked { get; set; }
    public double ExpectedHours { get; set; }
    public double AttendancePct { get; set; }

    public DateTimeOffset ComputedAt { get; set; }
}
