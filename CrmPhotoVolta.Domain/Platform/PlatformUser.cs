namespace CrmPhotoVolta.Domain.Platform;

public class PlatformUser : Common.EntityBase
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<PlatformUserRole> UserRoles { get; set; } = new List<PlatformUserRole>();
}
