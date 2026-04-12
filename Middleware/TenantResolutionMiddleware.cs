using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;

namespace CrmPhotoVoltaApis.Middleware;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var societyClaim = context.User.FindFirst(JwtClaimNames.SocietyId)?.Value;
            if (Guid.TryParse(societyClaim, out var societyId))
                tenantContext.SetCurrentSociety(societyId);
        }

        await _next(context);
    }
}
