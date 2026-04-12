using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Auth.Dtos;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenant;

    public AuthController(IAuthService auth, ICurrentUser currentUser, ITenantContext tenant)
    {
        _auth = auth;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var tokens = await _auth.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok(tokens));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var tokens = await _auth.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(tokens));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var tokens = await _auth.RefreshAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok(tokens));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        await _auth.LogoutAsync(request.RefreshToken, cancellationToken);
        return Ok(ApiResponse.Ok(new { }));
    }

    [Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var me = await _auth.GetMeAsync(userId, _tenant.CurrentSocietyId, cancellationToken);
        return Ok(ApiResponse.Ok(me));
    }

    [Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
    [HttpPost("switch-society")]
    public async Task<IActionResult> SwitchSociety([FromBody] SwitchSocietyRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var tokens = await _auth.SwitchSocietyAsync(userId, request, cancellationToken);
        return Ok(ApiResponse.Ok(tokens));
    }
}
