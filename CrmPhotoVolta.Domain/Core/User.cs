namespace CrmPhotoVolta.Domain.Core;

public class User : Common.EntityBase
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserSociety> UserSocieties { get; set; } = new List<UserSociety>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
