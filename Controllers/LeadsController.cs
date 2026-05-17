using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Leads;
using CrmPhotoVolta.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/leads")]
public sealed class LeadsController : TenantCrmControllerBase
{
    private readonly ILeadService _leads;
    private readonly ICurrentUser _currentUser;

    public LeadsController(ITenantContext tenant, ILeadService leads, ICurrentUser currentUser) : base(tenant)
    {
        _leads = leads;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var (items, meta) = await _leads.ListPagedAsync(societyId, actorId, query.ToRequest(), cancellationToken);
        return Ok(ApiResponse.OkPaged(items, meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var item = await _leads.GetAsync(societyId, actorId, id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateLeadRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var created = await _leads.CreateAsync(societyId, actorId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _leads.UpdateAsync(societyId, actorId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        await _leads.DeleteAsync(societyId, actorId, id, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }

    [HttpGet("{id:guid}/activities")]
    public async Task<IActionResult> ListActivities(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var list = await _leads.ListActivitiesAsync(societyId, actorId, id, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpPost("{id:guid}/activities")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddActivity(Guid id, [FromBody] AddLeadActivityRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var created = await _leads.AddActivityAsync(societyId, id, actorId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}/activities/{activityId:guid}")]
    public async Task<IActionResult> UpdateActivity(Guid id, Guid activityId, [FromBody] UpdateLeadActivityRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _leads.UpdateActivityAsync(societyId, id, activityId, actorId, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}/activities/{activityId:guid}")]
    public async Task<IActionResult> DeleteActivity(Guid id, Guid activityId, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        await _leads.DeleteActivityAsync(societyId, id, activityId, actorId, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignLeadRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _leads.AssignAsync(societyId, id, actorId, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPost("{id:guid}/convert")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Convert(Guid id, [FromBody] ConvertLeadRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var result = await _leads.ConvertAsync(societyId, id, actorId, request, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("{id:guid}/mark-won")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkWon(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var result = await _leads.MarkWonAsync(societyId, id, actorId, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("{id:guid}/mark-lost")]
    public async Task<IActionResult> MarkLost(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _leads.MarkLostAsync(societyId, id, actorId, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPost("{id:guid}/notes")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] AddLeadNoteRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var created = await _leads.AddNoteAsync(societyId, id, actorId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> Timeline(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var list = await _leads.GetTimelineAsync(societyId, actorId, id, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpGet("{id:guid}/journal")]
    public async Task<IActionResult> Journal(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var list = await _leads.GetJournalAsync(societyId, actorId, id, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    /// <summary>Force le recalcul LVI/SD (même logique qu’après activité / mise à jour).</summary>
    [HttpPost("{id:guid}/score")]
    public async Task<IActionResult> RecalculateScore(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _leads.RecalculateScoreAsync(societyId, actorId, id, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    /// <summary>
    /// Change manuellement la temperature (Hot/High/Medium/Low/Cold).
    /// Applique le score minimum et trace UpdatedById / UpdatedAt.
    /// </summary>
    [HttpPatch("{id:guid}/temperature")]
    public async Task<IActionResult> ChangeTemperature(Guid id, [FromBody] ChangeLeadTemperatureRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _leads.ChangeTemperatureAsync(societyId, id, actorId, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeLeadStatusRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _leads.ChangeStatusAsync(societyId, id, actorId, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPost("{id:guid}/tags")]
    public async Task<IActionResult> AddTag(Guid id, [FromBody] AddLeadTagRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _leads.AddTagAsync(societyId, actorId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}/tags/{tag}")]
    public async Task<IActionResult> RemoveTag(Guid id, string tag, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _leads.RemoveTagAsync(societyId, actorId, id, tag, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }
}
