namespace CrmPhotoVolta.Domain.Platform;

public class PlatformRole : Common.EntityBase
{
    public string Name { get; set; } = string.Empty;

    public ICollection<PlatformUserRole> UserRoles { get; set; } = new List<PlatformUserRole>();
    public ICollection<PlatformRolePermission> RolePermissions { get; set; } = new List<PlatformRolePermission>();
}
