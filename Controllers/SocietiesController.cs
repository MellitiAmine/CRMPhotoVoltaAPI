using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Societies;
using CrmPhotoVolta.Application.Societies.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/societies")]
public sealed class SocietiesController : ControllerBase
{
    private readonly ISocietyService _societies;
    private readonly ICurrentUser _currentUser;

    public SocietiesController(ISocietyService societies, ICurrentUser currentUser)
    {
        _societies = societies;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var list = await _societies.ListForUserAsync(userId, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var item = await _societies.GetAsync(userId, id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateSocietyRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var created = await _societies.CreateAsync(userId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSocietyRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _societies.UpdateAsync(userId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        await _societies.DeleteAsync(userId, id, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }
}
