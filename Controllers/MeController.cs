using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Me;
using CrmPhotoVolta.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/me")]
public sealed class MeController : TenantCrmControllerBase
{
    private readonly IMeWorkspaceService _me;
    private readonly ICurrentUser _currentUser;

    public MeController(ITenantContext tenant, IMeWorkspaceService me, ICurrentUser currentUser) : base(tenant)
    {
        _me = me;
        _currentUser = currentUser;
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> Tasks(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var list = await _me.GetMyTasksAsync(RequireSociety(), userId, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpGet("installations")]
    public async Task<IActionResult> Installations(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var list = await _me.GetMyInstallationsAsync(RequireSociety(), userId, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpGet("schedule")]
    public async Task<IActionResult> Schedule([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var list = await _me.GetMyScheduleAsync(RequireSociety(), userId, from, to, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }
}
