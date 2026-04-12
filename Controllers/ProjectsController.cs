using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/projects")]
public sealed class ProjectsController : TenantCrmControllerBase
{
    private readonly IProjectService _projects;

    public ProjectsController(ITenantContext tenant, IProjectService projects) : base(tenant)
    {
        _projects = projects;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PaginationQuery query,
        [FromQuery] Guid? clientId,
        CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var (items, meta) = await _projects.ListPagedAsync(societyId, clientId, query.ToRequest(), cancellationToken);
        return Ok(ApiResponse.OkPaged(items, meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var item = await _projects.GetAsync(societyId, id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var created = await _projects.CreateAsync(societyId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var updated = await _projects.UpdateAsync(societyId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        await _projects.DeleteAsync(societyId, id, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }
}
