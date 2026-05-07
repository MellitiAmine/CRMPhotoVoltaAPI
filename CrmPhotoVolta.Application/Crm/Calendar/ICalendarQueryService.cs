namespace CrmPhotoVolta.Application.Crm.Calendar;

public interface ICalendarQueryService
{
    /// <summary>
    /// Returns events for a society.
    /// When <paramref name="callerUserId"/> is supplied and the caller is NOT an admin,
    /// only events the caller created or is a participant in are returned.
    /// </summary>
    Task<IReadOnlyList<CalendarEventDto>> ListAsync(
        Guid societyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        Guid? technicianId,
        Guid? projectId,
        Guid? callerUserId,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
}

public sealed class CalendarEventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;

    /// <summary>"meeting" | "reminder" | "activity"</summary>
    public string Type { get; init; } = string.Empty;

    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
    public string? Description { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public List<Guid> Participants { get; init; } = new();
    public Guid? CreatedById { get; init; }
    public Guid? LeadId { get; init; }
}
