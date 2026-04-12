using CrmPhotoVolta.Domain.Common;

namespace CrmPhotoVolta.Domain.App;

public class InstallationChecklistItem : EntityBase
{
    public Guid InstallationId { get; set; }
    public Installation Installation { get; set; } = null!;

    public string Item { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
