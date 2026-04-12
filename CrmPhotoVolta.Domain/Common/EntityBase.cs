namespace CrmPhotoVolta.Domain.Common;

public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
    public bool IsDeleted { get; set; }
}
