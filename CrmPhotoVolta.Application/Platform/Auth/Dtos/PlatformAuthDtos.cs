namespace CrmPhotoVolta.Application.Platform.Auth.Dtos;

public sealed class PlatformLoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class PlatformLoginResult
{
    public string AccessToken { get; init; } = string.Empty;
    public Guid PlatformUserId { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public DateTimeOffset AccessTokenExpiresAt { get; init; }
}
