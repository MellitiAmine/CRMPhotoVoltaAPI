namespace CrmPhotoVolta.Domain.App;

public class CrmTask : SocietyScopedEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public CrmTaskStatus Status { get; set; } = CrmTaskStatus.Open;
    public LeadPriority Priority { get; set; } = LeadPriority.Low;
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
