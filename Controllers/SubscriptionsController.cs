using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Subscriptions;
using CrmPhotoVolta.Application.Subscriptions.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/subscriptions")]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptions;
    private readonly ITenantContext _tenant;

    public SubscriptionsController(ISubscriptionService subscriptions, ITenantContext tenant)
    {
        _subscriptions = subscriptions;
        _tenant = tenant;
    }

    [HttpGet("current")]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var current = await _subscriptions.GetCurrentAsync(societyId, cancellationToken);
        if (current is null)
            throw new AppException("SUBSCRIPTION_NOT_FOUND", "No active subscription for this society.", StatusCodes.Status404NotFound);
        return Ok(ApiResponse.Ok(current));
    }

    [HttpPost("upgrade")]
    public async Task<IActionResult> Upgrade([FromBody] UpgradeSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var upgraded = await _subscriptions.UpgradeAsync(societyId, request, cancellationToken);
        return Ok(ApiResponse.Ok(upgraded));
    }

    private Guid RequireSociety()
    {
        if (_tenant.CurrentSocietyId is { } sid)
            return sid;
        throw new AppException("TENANT_REQUIRED", "Society context is required (JWT claim society_id).", StatusCodes.Status403Forbidden);
    }
}
