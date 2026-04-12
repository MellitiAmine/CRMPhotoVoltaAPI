using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CrmPhotoVolta.Infrastructure.Auth;

public interface IPlatformJwtTokenService
{
    string CreateAccessToken(Guid platformUserId, string email, IReadOnlyList<string> roleNames);
}

public sealed class PlatformJwtTokenService : IPlatformJwtTokenService
{
    private readonly PlatformJwtOptions _options;
    private readonly JwtSecurityTokenHandler _handler = new();

    public PlatformJwtTokenService(IOptions<PlatformJwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateAccessToken(Guid platformUserId, string email, IReadOnlyList<string> roleNames)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, platformUserId.ToString()),
            new(JwtRegisteredClaimNames.Email, email)
        };

        foreach (var r in roleNames)
            claims.Add(new Claim(ClaimTypes.Role, r));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return _handler.WriteToken(token);
    }
}
