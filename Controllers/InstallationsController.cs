using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Installations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/installations")]
public sealed class InstallationsController : TenantCrmControllerBase
{
    private readonly IInstallationWorkflowService _installations;

    public InstallationsController(ITenantContext tenant, IInstallationWorkflowService installations) : base(tenant)
    {
        _installations = installations;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _installations.GetAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        var updated = await _installations.StartAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var updated = await _installations.CompleteAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPut("{id:guid}/checklist")]
    public async Task<IActionResult> UpdateChecklist(Guid id, [FromBody] UpdateInstallationChecklistRequest request, CancellationToken cancellationToken)
    {
        var list = await _installations.UpdateChecklistAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpPost("{id:guid}/photos")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddPhoto(Guid id, [FromBody] AddInstallationPhotoRequest request, CancellationToken cancellationToken)
    {
        var created = await _installations.AddPhotoAsync(RequireSociety(), id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }
}
