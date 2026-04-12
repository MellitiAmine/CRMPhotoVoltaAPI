namespace CrmPhotoVolta.Domain.Core;

public class RefreshToken : Common.EntityBase
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid SocietyId { get; set; }

    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
