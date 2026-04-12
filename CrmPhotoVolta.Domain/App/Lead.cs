namespace CrmPhotoVolta.Domain.App;

public class Lead : SocietyScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = "New";
    public Guid? AssignedToUserId { get; set; }

    public ICollection<LeadActivity> Activities { get; set; } = new List<LeadActivity>();
    public ICollection<Deal> Deals { get; set; } = new List<Deal>();
}
