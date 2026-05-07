namespace CrmPhotoVolta.Application.Crm.Calendar;

public interface ICalendarCommandService
{
    Task<CalendarEventDto> CreateAsync(Guid societyId, Guid creatorId, CreateCalendarEventRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid societyId, Guid eventId, Guid requestingUserId, bool requestingUserIsAdmin, CancellationToken cancellationToken = default);
}

public sealed class CreateCalendarEventRequest
{
    public string Title { get; init; } = string.Empty;

    /// <summary>"meeting" | "reminder" | "activity"</summary>
    public string Type { get; init; } = string.Empty;

    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
    public string? Description { get; init; }

    /// <summary>User IDs to invite (must be in the same society).</summary>
    public List<Guid> Participants { get; init; } = new();
}
