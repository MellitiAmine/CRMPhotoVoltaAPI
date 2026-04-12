using CrmPhotoVolta.Domain.Common;

namespace CrmPhotoVolta.Domain.App;

public class InstallationPhoto : EntityBase
{
    public Guid InstallationId { get; set; }
    public Installation Installation { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
}
