using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Roles;
using CrmPhotoVolta.Application.Roles.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Authorize(Policy = SocietyPolicies.Admin)]
[Route("api/v1/roles")]
public sealed class RolesController : TenantCrmControllerBase
{
    private readonly IRoleService _roles;

    public RolesController(ITenantContext tenant, IRoleService roles) : base(tenant)
    {
        _roles = roles;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var list = await _roles.ListAsync(societyId, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var created = await _roles.CreateAsync(societyId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var updated = await _roles.UpdateAsync(societyId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        await _roles.DeleteAsync(societyId, id, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }

    [HttpGet("{id:guid}/permissions")]
    public async Task<IActionResult> GetPermissions(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var list = await _roles.GetPermissionsAsync(societyId, id, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> ReplacePermissions(Guid id, [FromBody] ReplaceRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        await _roles.ReplacePermissionsAsync(societyId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok(new { updated = true }));
    }
}
