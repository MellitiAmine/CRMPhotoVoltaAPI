namespace CrmPhotoVolta.Domain.App;

public class ProjectStage : SocietyScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }

    public ICollection<ProjectStageTracking> Trackings { get; set; } = new List<ProjectStageTracking>();
}
