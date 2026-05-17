namespace CrmPhotoVolta.Domain.App;

public class ProjectTimelineEvent : SocietyScopedEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public ProjectTimelineEventType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }
}
