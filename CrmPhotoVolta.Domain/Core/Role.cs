namespace CrmPhotoVolta.Domain.Core;

public class Role : Common.EntityBase
{
    public Guid? SocietyId { get; set; }
    public Society? Society { get; set; }

    public string Name { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }

    public ICollection<UserSociety> UserSocieties { get; set; } = new List<UserSociety>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
