using CrmPhotoVolta.Domain.Common;

namespace CrmPhotoVolta.Domain.App;

public class ProjectStageTracking : EntityBase
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid StageId { get; set; }
    public ProjectStage Stage { get; set; } = null!;

    public string Status { get; set; } = "Pending";
    public DateTimeOffset? CompletedAt { get; set; }
}
