namespace CrmPhotoVolta.Domain.App;

public class CrmTask : SocietyScopedEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public Guid? AssignedToUserId { get; set; }
    public CrmTaskStatus Status { get; set; } = CrmTaskStatus.Open;
    public DateOnly? DueDate { get; set; }
}
