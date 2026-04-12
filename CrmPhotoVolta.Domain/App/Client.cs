namespace CrmPhotoVolta.Domain.App;

public class Client : SocietyScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public Guid? UserId { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
