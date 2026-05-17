namespace CrmPhotoVolta.Domain.App;

public class Project : SocietyScopedEntity
{
    public Guid? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public Guid? DealId { get; set; }
    public Deal? Deal { get; set; }

    public Guid? QuoteId { get; set; }
    public Quote? Quote { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Address { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.New;
    public LeadPriority Priority { get; set; } = LeadPriority.Low;
    public string? Notes { get; set; }

    public decimal? SystemSizeKw { get; set; }
    public decimal? EstimatedProduction { get; set; }
    public string? RoofType { get; set; }
    public string? InstallationType { get; set; }
    public int? PanelCount { get; set; }
    public int? InverterCount { get; set; }

    public decimal TotalHt { get; set; }
    public decimal TotalTva { get; set; }
    public decimal TotalTtc { get; set; }
    public decimal? EstimatedMargin { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? ExpectedInstallationDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public Guid? ManagerUserId { get; set; }
    public Guid? CommercialUserId { get; set; }
    public Guid? TechnicianUserId { get; set; }
    public int ProgressPercent { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }

    public ICollection<ProjectStageTracking> StageTrackings { get; set; } = new List<ProjectStageTracking>();
    public ICollection<CrmTask> Tasks { get; set; } = new List<CrmTask>();
    public ICollection<Installation> Installations { get; set; } = new List<Installation>();
    public ICollection<ProjectTimelineEvent> TimelineEvents { get; set; } = new List<ProjectTimelineEvent>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<ProjectDocument> ProjectDocuments { get; set; } = new List<ProjectDocument>();
}
