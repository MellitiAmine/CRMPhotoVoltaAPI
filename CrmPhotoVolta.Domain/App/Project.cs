namespace CrmPhotoVolta.Domain.App;

public class Project : SocietyScopedEntity
{
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public Guid? DealId { get; set; }
    public Deal? Deal { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string Status { get; set; } = "Planned";
    public decimal? SystemSizeKw { get; set; }
    public decimal? EstimatedProduction { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public Guid? ManagerUserId { get; set; }
    public Guid? TechnicianUserId { get; set; }
    public int ProgressPercent { get; set; }

    public ICollection<ProjectStageTracking> StageTrackings { get; set; } = new List<ProjectStageTracking>();
    public ICollection<CrmTask> Tasks { get; set; } = new List<CrmTask>();
    public ICollection<Installation> Installations { get; set; } = new List<Installation>();
}
