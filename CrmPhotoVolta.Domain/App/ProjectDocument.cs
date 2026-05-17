namespace CrmPhotoVolta.Domain.App;

public class ProjectDocument : SocietyScopedEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public ProjectDocumentType Type { get; set; } = ProjectDocumentType.Other;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public Guid? UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
