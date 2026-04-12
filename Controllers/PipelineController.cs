using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Pipeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/pipeline/stages")]
public sealed class PipelineController : TenantCrmControllerBase
{
    private readonly IPipelineStageService _pipeline;

    public PipelineController(ITenantContext tenant, IPipelineStageService pipeline) : base(tenant)
    {
        _pipeline = pipeline;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var list = await _pipeline.ListAsync(RequireSociety(), cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePipelineStageRequest request, CancellationToken cancellationToken)
    {
        var created = await _pipeline.CreateAsync(RequireSociety(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePipelineStageRequest request, CancellationToken cancellationToken)
    {
        var updated = await _pipeline.UpdateAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _pipeline.DeleteAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }
}
