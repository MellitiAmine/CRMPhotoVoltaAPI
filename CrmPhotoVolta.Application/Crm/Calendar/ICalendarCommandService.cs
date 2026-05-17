namespace CrmPhotoVolta.Application.Crm.Calendar;

public interface ICalendarCommandService
{
    Task<CalendarEventDto> CreateAsync(Guid societyId, Guid creatorId, CreateCalendarEventRequest request, CancellationToken cancellationToken = default);
    Task<CalendarEventDto> UpdateAsync(Guid societyId, Guid eventId, Guid requestingUserId, UpdateCalendarEventRequest request, CancellationToken cancellationToken = default);
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

    /// <summary>Primary assignee; must be one of <see cref="Participants"/> when set. Defaults to first participant.</summary>
    public Guid? AssignedToUserId { get; init; }

    /// <summary>Optional lead (pipeline deal) this event is tied to.</summary>
    public Guid? LeadId { get; init; }
}

public sealed class UpdateCalendarEventRequest
{
    public string Title { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
    public string? Description { get; init; }
    public List<Guid> Participants { get; init; } = new();

    /// <summary>Primary assignee; must be one of <see cref="Participants"/> when set.</summary>
    public Guid? AssignedToUserId { get; init; }

    public Guid? LeadId { get; init; }
}
