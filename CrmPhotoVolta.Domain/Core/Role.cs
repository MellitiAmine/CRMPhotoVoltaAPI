namespace CrmPhotoVolta.Domain.Core;

public class Role : Common.EntityBase
{
    public Guid? SocietyId { get; set; }
    public Society? Society { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Stable classification; <see cref="Name"/> remains the tenant-facing label.</summary>
    public RoleType RoleType { get; set; } = RoleType.Unknown;

    public bool IsSystemRole { get; set; }

    public ICollection<UserSociety> UserSocieties { get; set; } = new List<UserSociety>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
