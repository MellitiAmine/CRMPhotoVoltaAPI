using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Platform.Auth;
using CrmPhotoVolta.Application.Platform.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/platform/auth")]
public sealed class PlatformAuthController : ControllerBase
{
    private readonly IPlatformAuthService _auth;

    public PlatformAuthController(IPlatformAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] PlatformLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }
}
