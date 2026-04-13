namespace CrmPhotoVolta.Domain.App;

public class Deal : SocietyScopedEntity
{
    public Guid? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public string Title { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public string Stage { get; set; } = DealStages.New;
    public Guid? AssignedToUserId { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
