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

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PaginationQuery query,
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? technicianId,
        CancellationToken cancellationToken)
    {
        var (items, meta) = await _installations.ListPagedAsync(
            RequireSociety(), projectId, technicianId, query.ToRequest(), cancellationToken);
        return Ok(ApiResponse.OkPaged(items, meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _installations.GetAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateInstallationRequest request, CancellationToken cancellationToken)
    {
        var created = await _installations.CreateAsync(RequireSociety(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInstallationRequest request, CancellationToken cancellationToken)
    {
        var updated = await _installations.UpdateAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
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

    /// <summary>Returns all checklist items for an installation (creation order).</summary>
    [HttpGet("{id:guid}/checklist")]
    public async Task<IActionResult> ListChecklist(Guid id, CancellationToken cancellationToken)
    {
        var list = await _installations.ListChecklistAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    /// <summary>Adds a single checklist item.</summary>
    [HttpPost("{id:guid}/checklist")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateChecklistItem(
        Guid id,
        [FromBody] CreateInstallationChecklistItemRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _installations.CreateChecklistItemAsync(RequireSociety(), id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    /// <summary>Seeds the default checklist when the installation has no items yet.</summary>
    [HttpPost("{id:guid}/checklist/initialize")]
    public async Task<IActionResult> InitializeChecklist(
        Guid id,
        [FromBody] InitializeInstallationChecklistRequest? request,
        CancellationToken cancellationToken)
    {
        var list = await _installations.InitializeChecklistAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    /// <summary>Bulk-updates completion status (and optionally labels) for multiple items.</summary>
    [HttpPut("{id:guid}/checklist")]
    public async Task<IActionResult> UpdateChecklist(Guid id, [FromBody] UpdateInstallationChecklistRequest request, CancellationToken cancellationToken)
    {
        var list = await _installations.UpdateChecklistAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    /// <summary>Updates a single checklist item (label and/or completed flag).</summary>
    [HttpPut("{id:guid}/checklist/{itemId:guid}")]
    public async Task<IActionResult> UpdateChecklistItem(
        Guid id,
        Guid itemId,
        [FromBody] UpdateInstallationChecklistItemRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _installations.UpdateChecklistItemAsync(RequireSociety(), id, itemId, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    /// <summary>Soft-deletes a checklist item.</summary>
    [HttpDelete("{id:guid}/checklist/{itemId:guid}")]
    public async Task<IActionResult> DeleteChecklistItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        await _installations.DeleteChecklistItemAsync(RequireSociety(), id, itemId, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }

    [HttpGet("{id:guid}/photos")]
    public async Task<IActionResult> ListPhotos(Guid id, CancellationToken cancellationToken)
    {
        var list = await _installations.ListPhotosAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpPost("{id:guid}/photos")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return UnprocessableEntity(ApiResponse.Fail("VALIDATION_ERROR", "File is required.", null));

        await using var stream = file.OpenReadStream();
        var created = await _installations.UploadPhotoAsync(
            RequireSociety(),
            id,
            file.FileName,
            file.ContentType,
            file.Length,
            stream,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    public async Task<IActionResult> DeletePhoto(Guid id, Guid photoId, CancellationToken cancellationToken)
    {
        await _installations.DeletePhotoAsync(RequireSociety(), id, photoId, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }
}
