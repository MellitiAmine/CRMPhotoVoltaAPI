namespace CrmPhotoVolta.Domain.App;

public class Installation : SocietyScopedEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid TechnicianId { get; set; }
    public DateOnly Date { get; set; }
    public InstallationStatus Status { get; set; } = InstallationStatus.Scheduled;

    public ICollection<InstallationChecklistItem> Checklist { get; set; } = new List<InstallationChecklistItem>();
    public ICollection<InstallationPhoto> Photos { get; set; } = new List<InstallationPhoto>();
}
