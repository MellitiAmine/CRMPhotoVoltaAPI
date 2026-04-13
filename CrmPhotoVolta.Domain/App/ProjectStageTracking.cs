namespace CrmPhotoVolta.Domain.App;

public class ProjectStageTracking : SocietyScopedEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid StageId { get; set; }
    public ProjectStage Stage { get; set; } = null!;

    public ProjectStageTrackingStatus Status { get; set; } = ProjectStageTrackingStatus.Pending;
    public DateTimeOffset? CompletedAt { get; set; }
}
