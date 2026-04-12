using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CrmPhotoVolta.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateAccessToken(Guid userId, string email, Guid societyId, IReadOnlyDictionary<string, string>? extraClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtClaimNames.Sub, userId.ToString()),
            new(JwtClaimNames.Email, email),
            new(JwtClaimNames.SocietyId, societyId.ToString())
        };

        if (extraClaims is not null)
        {
            foreach (var kv in extraClaims)
                claims.Add(new Claim(kv.Key, kv.Value));
        }

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

    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = key,
            ValidateLifetime = false
        };

        try
        {
            return _handler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }
}
