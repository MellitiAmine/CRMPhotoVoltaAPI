using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Platform.Auth;
using CrmPhotoVolta.Application.Platform.Auth.Dtos;
using CrmPhotoVolta.Infrastructure.Data.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CrmPhotoVolta.Infrastructure.Auth;

public sealed class PlatformAuthService : IPlatformAuthService
{
    private readonly PlatformDbContext _db;
    private readonly IPlatformJwtTokenService _jwt;
    private readonly PlatformJwtOptions _jwtOptions;

    public PlatformAuthService(
        PlatformDbContext db,
        IPlatformJwtTokenService jwt,
        IOptions<PlatformJwtOptions> jwtOptions)
    {
        _db = db;
        _jwt = jwt;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<PlatformLoginResult> LoginAsync(PlatformLoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.PlatformUsers
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.PlatformRole)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new AppException("INVALID_CREDENTIALS", "Invalid email or password.", 401);

        if (!user.IsActive)
            throw new AppException("USER_DISABLED", "Platform user is disabled.", 403);

        var roles = user.UserRoles
            .Where(x => !x.IsDeleted && x.PlatformRole is { IsDeleted: false })
            .Select(x => x.PlatformRole!.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var token = _jwt.CreateAccessToken(user.Id, user.Email, roles);

        return new PlatformLoginResult
        {
            AccessToken = token,
            PlatformUserId = user.Id,
            Roles = roles,
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes)
        };
    }
}
