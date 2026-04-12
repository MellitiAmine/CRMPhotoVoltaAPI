namespace CrmPhotoVolta.Domain.App;

public class Notification : SocietyScopedEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset? ReadAt { get; set; }
}
