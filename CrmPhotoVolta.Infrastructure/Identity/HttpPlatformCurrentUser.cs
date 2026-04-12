using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CrmPhotoVolta.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace CrmPhotoVolta.Infrastructure.Identity;

public sealed class HttpPlatformCurrentUser : IPlatformCurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpPlatformCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? PlatformUserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email);

    public IReadOnlyList<string> RoleNames =>
        _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        ?? (IReadOnlyList<string>)Array.Empty<string>();
}
