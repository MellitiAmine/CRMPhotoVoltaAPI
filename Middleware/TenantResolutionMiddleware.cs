using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using Microsoft.AspNetCore.Http;

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
            if (context.Request.Path.StartsWithSegments("/api/v1/platform"))
            {
                await _next(context);
                return;
            }

            var societyClaim = context.User.FindFirst(JwtClaimNames.SocietyId)?.Value;
            if (!Guid.TryParse(societyClaim, out var societyId))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    error = new { code = "TENANT_REQUIRED", message = "society_id claim is required." }
                });
                return;
            }

            tenantContext.SetCurrentSociety(societyId);
        }

        await _next(context);
    }
}
