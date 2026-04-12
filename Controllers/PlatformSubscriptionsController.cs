using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Platform.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.PlatformJwt)]
[Route("api/v1/platform/subscriptions")]
public sealed class PlatformSubscriptionsController : ControllerBase
{
    private readonly IPlatformSubscriptionAdminService _subscriptions;

    public PlatformSubscriptionsController(IPlatformSubscriptionAdminService subscriptions)
    {
        _subscriptions = subscriptions;
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePlatformSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _subscriptions.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }
}
