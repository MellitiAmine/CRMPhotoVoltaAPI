namespace CrmPhotoVolta.Domain.App;

public class CalendarEvent : SocietyScopedEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>"meeting" | "reminder" | "activity"</summary>
    public string Type { get; set; } = string.Empty;

    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }

    public string? Description { get; set; }

    /// <summary>Primary assignee (kept for backward compatibility with technician/project filters).</summary>
    public Guid? AssignedToUserId { get; set; }

    /// <summary>User IDs of all invited participants — stored as a JSON array in the DB.</summary>
    public List<Guid> Participants { get; set; } = new();
}
