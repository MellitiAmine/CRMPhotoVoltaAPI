using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Installations;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/installations")]
public sealed class InstallationsController : TenantCrmControllerBase
{
    private static readonly string[] PlanningAdminRoles = ["Admin", "Manager"];

    private readonly IInstallationWorkflowService _installations;
    private readonly ICurrentUser _currentUser;
    private readonly CoreDbContext _core;

    public InstallationsController(
        ITenantContext tenant,
        IInstallationWorkflowService installations,
        ICurrentUser currentUser,
        CoreDbContext core) : base(tenant)
    {
        _installations = installations;
        _currentUser   = currentUser;
        _core          = core;
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

    /// <summary>Planning global — Admin / Manager uniquement.</summary>
    [HttpGet("planning")]
    public async Task<IActionResult> Planning(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? technicianId,
        [FromQuery] InstallationStatus? status,
        CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var userId = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);

        if (!await HasPlanningAdminAccessAsync(societyId, userId, cancellationToken))
            throw new AppException("FORBIDDEN", "Only Admin or Manager can view the full technician planning.", 403);

        var planning = await _installations.GetPlanningAsync(
            societyId,
            new InstallationPlanningQuery(from, to, technicianId, status),
            restrictToTechnicianUserId: null,
            cancellationToken);

        return Ok(ApiResponse.Ok(planning));
    }

    /// <summary>Mon planning — installations assignées à l'utilisateur connecté.</summary>
    [HttpGet("my-planning")]
    public async Task<IActionResult> MyPlanning(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] InstallationStatus? status,
        CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var userId = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);

        var planning = await _installations.GetPlanningAsync(
            societyId,
            new InstallationPlanningQuery(from, to, TechnicianId: null, status),
            restrictToTechnicianUserId: userId,
            cancellationToken);

        return Ok(ApiResponse.Ok(planning));
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

    [HttpGet("{id:guid}/checklist")]
    public async Task<IActionResult> ListChecklist(Guid id, CancellationToken cancellationToken)
    {
        var list = await _installations.ListChecklistAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

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

    [HttpPost("{id:guid}/checklist/initialize")]
    public async Task<IActionResult> InitializeChecklist(
        Guid id,
        [FromBody] InitializeInstallationChecklistRequest? request,
        CancellationToken cancellationToken)
    {
        var list = await _installations.InitializeChecklistAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpPut("{id:guid}/checklist")]
    public async Task<IActionResult> UpdateChecklist(Guid id, [FromBody] UpdateInstallationChecklistRequest request, CancellationToken cancellationToken)
    {
        var list = await _installations.UpdateChecklistAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

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

    private async Task<bool> HasPlanningAdminAccessAsync(Guid societyId, Guid userId, CancellationToken cancellationToken) =>
        await _core.UserSocieties
            .AsNoTracking()
            .Include(x => x.Role)
            .AnyAsync(x =>
                !x.IsDeleted &&
                x.UserId == userId &&
                x.SocietyId == societyId &&
                x.Role != null &&
                PlanningAdminRoles.Contains(x.Role.Name),
                cancellationToken);
}
