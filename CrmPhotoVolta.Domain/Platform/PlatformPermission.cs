namespace CrmPhotoVolta.Domain.Platform;

public class PlatformPermission : Common.EntityBase
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<PlatformRolePermission> RolePermissions { get; set; } = new List<PlatformRolePermission>();
}
