using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Auth;

public sealed class SocietyRoleRequirement : IAuthorizationRequirement
{
    public SocietyRoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }

    public IReadOnlyList<string> AllowedRoles { get; }
}

public sealed class SocietyRoleHandler : AuthorizationHandler<SocietyRoleRequirement>
{
    private readonly CoreDbContext _core;

    public SocietyRoleHandler(CoreDbContext core)
    {
        _core = core;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SocietyRoleRequirement requirement)
    {
        var sub = context.User.FindFirst(JwtClaimNames.Sub)?.Value;
        var societyClaim = context.User.FindFirst(JwtClaimNames.SocietyId)?.Value;

        if (!Guid.TryParse(sub, out var userId) || !Guid.TryParse(societyClaim, out var societyId))
            return;

        var hasRole = await _core.UserSocieties
            .AsNoTracking()
            .Include(x => x.Role)
            .AnyAsync(x =>
                !x.IsDeleted &&
                x.UserId == userId &&
                x.SocietyId == societyId &&
                x.Role != null &&
                requirement.AllowedRoles.Contains(x.Role.Name));

        if (hasRole)
            context.Succeed(requirement);
    }
}

