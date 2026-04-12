namespace CrmPhotoVolta.Domain.App;

public class PipelineStage : SocietyScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}
