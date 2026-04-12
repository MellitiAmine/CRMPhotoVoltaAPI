using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Platform;
using CrmPhotoVolta.Application.Platform.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.PlatformJwt)]
[Route("api/v1/platform/subscription-plans")]
public sealed class PlatformSubscriptionPlansController : ControllerBase
{
    private readonly IPlatformSubscriptionPlanService _plans;

    public PlatformSubscriptionPlansController(IPlatformSubscriptionPlanService plans)
    {
        _plans = plans;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var list = await _plans.ListAsync(cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var created = await _plans.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var updated = await _plans.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }
}
