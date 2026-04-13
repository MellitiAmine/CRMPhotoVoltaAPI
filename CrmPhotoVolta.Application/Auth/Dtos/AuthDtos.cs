namespace CrmPhotoVolta.Application.Auth.Dtos;

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string SocietyName { get; init; } = string.Empty;
}

public sealed class RefreshRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class AuthTokensResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; init; }

    public Guid UserId { get; init; }
    public Guid SocietyId { get; init; }
    public string Role { get; init; } = string.Empty;
}

public sealed class MeResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public Guid? CurrentSocietyId { get; init; }
    public IReadOnlyList<UserSocietySummaryDto> Societies { get; init; } = Array.Empty<UserSocietySummaryDto>();
}

public sealed class UserSocietySummaryDto
{
    public Guid SocietyId { get; init; }
    public string SocietyName { get; init; } = string.Empty;
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
}
