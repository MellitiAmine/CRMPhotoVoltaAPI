using System.Security.Claims;

namespace CrmPhotoVolta.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateAccessToken(Guid userId, string email, Guid societyId, IReadOnlyDictionary<string, string>? extraClaims = null);
    string CreateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
