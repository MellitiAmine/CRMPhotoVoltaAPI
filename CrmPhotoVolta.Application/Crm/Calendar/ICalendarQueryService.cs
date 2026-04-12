namespace CrmPhotoVolta.Application.Crm.Calendar;

public interface ICalendarQueryService
{
    Task<IReadOnlyList<CalendarEventDto>> ListAsync(Guid societyId, DateTimeOffset? from, DateTimeOffset? to, Guid? technicianId, Guid? projectId, CancellationToken cancellationToken = default);
}

public sealed class CalendarEventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
    public Guid? AssignedToUserId { get; init; }
}
