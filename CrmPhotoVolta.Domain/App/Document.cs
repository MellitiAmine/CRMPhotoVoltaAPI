namespace CrmPhotoVolta.Domain.App;

public class Document : SocietyScopedEntity
{
    public Guid? ProjectId { get; set; }
    public Guid? ClientId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
}
