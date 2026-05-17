namespace CrmPhotoVolta.Application.Crm.Calendar;

/// <summary>Active tenant user eligible for calendar invites / sidebar (scoped to society membership only).</summary>
public sealed record CalendarParticipantCandidateDto(
    Guid Id,
    string FullName,
    string Email,
    string RoleName,
    /// <summary><c>CrmPhotoVolta.Domain.Core.RoleType</c> numeric value (Commercial = 3).</summary>
    int RoleType);

public interface ICalendarQueryService
{
    /// <summary>
    /// All active users in this society — not filtered by commercial hierarchy (<c>GET /commercials</c> list is subtree-scoped).
    /// Use for participant pickers so every licensed member can receive an invite.
    /// </summary>
    Task<IReadOnlyList<CalendarParticipantCandidateDto>> ListParticipantCandidatesAsync(
        Guid societyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns events for a society.
    /// When <paramref name="callerUserId"/> is supplied and the caller is NOT an admin,
    /// only events involving the caller's commercial subtree (creator / assignee / participant) are returned.
    /// When the caller <b>is</b> an admin, all events in the society (within filters) are returned.
    /// </summary>
    Task<IReadOnlyList<CalendarEventDto>> ListAsync(
        Guid societyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        Guid? technicianId,
        Guid? projectId,
        Guid? leadId,
        Guid? callerUserId,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Events where <paramref name="targetUserId"/> is assignee or in <c>Participants</c>.
    /// Caller authorization must be done by the controller (e.g. commercial profile access).
    /// </summary>
    Task<IReadOnlyList<CalendarEventDto>> ListEventsForSocietyUserAsync(
        Guid societyId,
        Guid targetUserId,
        DateTimeOffset? from,
        DateTimeOffset? to,
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
