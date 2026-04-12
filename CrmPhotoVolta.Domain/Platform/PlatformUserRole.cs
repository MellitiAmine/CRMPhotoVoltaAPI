namespace CrmPhotoVolta.Domain.Platform;

public class PlatformUserRole : Common.EntityBase
{
    public Guid PlatformUserId { get; set; }
    public PlatformUser PlatformUser { get; set; } = null!;

    public Guid PlatformRoleId { get; set; }
    public PlatformRole PlatformRole { get; set; } = null!;
}
