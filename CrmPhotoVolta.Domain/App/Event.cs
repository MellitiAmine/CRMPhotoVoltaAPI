namespace CrmPhotoVolta.Domain.App;

public class CalendarEvent : SocietyScopedEntity
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
}
