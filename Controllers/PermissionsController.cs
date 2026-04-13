using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Authorize(Policy = SocietyPolicies.Admin)]
[Route("api/v1/permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissions;

    public PermissionsController(IPermissionService permissions)
    {
        _permissions = permissions;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var list = await _permissions.ListAsync(cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }
}
