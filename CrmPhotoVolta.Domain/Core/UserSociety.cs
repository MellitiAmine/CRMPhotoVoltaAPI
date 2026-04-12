namespace CrmPhotoVolta.Domain.Core;

public class UserSociety : Common.EntityBase
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
