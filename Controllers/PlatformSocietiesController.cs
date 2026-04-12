using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Platform.Societies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.PlatformJwt)]
[Route("api/v1/platform/societies")]
public sealed class PlatformSocietiesController : ControllerBase
{
    private readonly IPlatformSocietyService _societies;

    public PlatformSocietiesController(IPlatformSocietyService societies)
    {
        _societies = societies;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var list = await _societies.ListAllAsync(cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _societies.GetAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePlatformSocietyRequest request, CancellationToken cancellationToken)
    {
        var created = await _societies.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlatformSocietyRequest request, CancellationToken cancellationToken)
    {
        var updated = await _societies.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _societies.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }
}
