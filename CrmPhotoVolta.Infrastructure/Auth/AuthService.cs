using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Auth.Dtos;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data;
using CrmPhotoVolta.Infrastructure.Data.Core;
using CrmPhotoVolta.Infrastructure.Data.Platform;
using CrmPhotoVolta.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CrmPhotoVolta.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly CoreDbContext _db;
    private readonly PlatformDbContext _platformDb;
    private readonly IJwtTokenService _jwt;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        CoreDbContext db,
        PlatformDbContext platformDb,
        IJwtTokenService jwt,
        IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _platformDb = platformDb;
        _jwt = jwt;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthTokensResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken))
            throw new AppException("EMAIL_IN_USE", "Email is already registered.", 409);

        var platformEmails = await PlatformEmailRegistry.LoadLowercaseEmailsAsync(_platformDb, cancellationToken);
        if (PlatformEmailRegistry.Contains(platformEmails, normalizedEmail))
            throw new AppException("EMAIL_RESERVED", "This email is reserved for platform access.", 409);

        await DatabaseSeeder.EnsureSeedAsync(_db, cancellationToken);

        var plan = await DatabaseSeeder.GetDefaultRegistrationPlanAsync(_db, cancellationToken);

        var society = new Society
        {
            Name = request.SocietyName.Trim(),
            SubscriptionPlanId = plan.Id,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Societies.Add(society);
        await _db.SaveChangesAsync(cancellationToken);

        var adminRole = await RoleBootstrapper.CreateAdminRoleAsync(_db, society.Id, cancellationToken);

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Phone = request.Phone,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        _db.UserSocieties.Add(new UserSociety
        {
            UserId = user.Id,
            SocietyId = society.Id,
            RoleId = adminRole.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        _db.Subscriptions.Add(new Subscription
        {
            SocietyId = society.Id,
            PlanId = plan.Id,
            StartDate = start,
            EndDate = SubscriptionPeriodCalculator.ComputeEndDate(start, plan),
            Status = SubscriptionStatuses.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, society.Id, cancellationToken);
    }

    public async Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .Include(x => x.UserSocieties)
            .ThenInclude(x => x.Society)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new AppException("INVALID_CREDENTIALS", "Invalid email or password.", 401);

        if (!user.IsActive)
            throw new AppException("USER_DISABLED", "User is disabled.", 403);

        var activeMemberships = user.UserSocieties
            .Where(x => !x.IsDeleted && x.Society is { IsDeleted: false, IsActive: true })
            .OrderBy(x => x.Society!.Name)
            .ToList();

        if (activeMemberships.Count == 0)
            throw new AppException("NO_SOCIETY_ACCESS", "User is not assigned to any active society.", 403);

        if (activeMemberships.Count > 1)
            throw new AppException(
                "MULTI_SOCIETY_NOT_ALLOWED",
                "This account is linked to multiple societies, which is forbidden by current security policy. Contact platform support to clean memberships.",
                403);

        var membership = activeMemberships[0];

        return await IssueTokensAsync(user, membership.SocietyId, cancellationToken);
    }

    public async Task<AuthTokensResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = request.RefreshToken.Trim();
        var existing = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == tokenHash, cancellationToken);

        if (existing is null || existing.RevokedAt is not null || existing.IsDeleted)
            throw new AppException("INVALID_REFRESH_TOKEN", "Refresh token is invalid.", 401);

        if (existing.ExpiresAt < DateTimeOffset.UtcNow)
            throw new AppException("REFRESH_TOKEN_EXPIRED", "Refresh token expired.", 401);

        existing.RevokedAt = DateTimeOffset.UtcNow;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        return await IssueTokensAsync(existing.User, existing.SocietyId, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var existing = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Token == refreshToken.Trim(), cancellationToken);

        if (existing is null)
            return;

        existing.RevokedAt = DateTimeOffset.UtcNow;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MeResponse> GetMeAsync(Guid userId, Guid? societyId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(x => x.UserSocieties)
            .ThenInclude(x => x.Society)
            .Include(x => x.UserSocieties)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppException("USER_NOT_FOUND", "User not found.", 404);

        var societies = user.UserSocieties
            .Where(x => !x.IsDeleted && x.Society is { IsDeleted: false })
            .Select(x => new UserSocietySummaryDto
            {
                SocietyId = x.SocietyId,
                SocietyName = x.Society!.Name,
                RoleId = x.RoleId,
                RoleName = x.Role!.Name
            })
            .OrderBy(x => x.SocietyName)
            .ToList();

        if (societies.Count > 1)
            throw new AppException(
                "MULTI_SOCIETY_NOT_ALLOWED",
                "This account is linked to multiple societies, which is forbidden by current security policy. Contact platform support to clean memberships.",
                403);

        return new MeResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            CurrentSocietyId = societyId,
            Societies = societies
        };
    }

    private async Task<AuthTokensResponse> IssueTokensAsync(User user, Guid societyId, CancellationToken cancellationToken)
    {
        var roleName = await GetRoleNameAsync(user.Id, societyId, cancellationToken);
        var accessToken = _jwt.CreateAccessToken(user.Id, user.Email, societyId, null);

        var refresh = _jwt.CreateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            SocietyId = societyId,
            Token = refresh,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new AuthTokensResponse
        {
            AccessToken = accessToken,
            RefreshToken = refresh,
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            UserId = user.Id,
            SocietyId = societyId,
            Role = roleName
        };
    }

    private async Task<string> GetRoleNameAsync(Guid userId, Guid societyId, CancellationToken cancellationToken)
    {
        var membership = await _db.UserSocieties
            .AsNoTracking()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.SocietyId == societyId && !x.IsDeleted,
                cancellationToken);

        return membership?.Role?.Name ?? "Unknown";
    }
}
