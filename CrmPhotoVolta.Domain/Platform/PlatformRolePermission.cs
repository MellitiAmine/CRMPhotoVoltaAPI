namespace CrmPhotoVolta.Domain.Platform;

public class PlatformRolePermission : Common.EntityBase
{
    public Guid PlatformRoleId { get; set; }
    public PlatformRole PlatformRole { get; set; } = null!;

    public Guid PlatformPermissionId { get; set; }
    public PlatformPermission PlatformPermission { get; set; } = null!;
}
