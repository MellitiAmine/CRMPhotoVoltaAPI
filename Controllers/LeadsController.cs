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
        var (items, meta) = await _leads.ListPagedAsync(societyId, query.ToRequest(), cancellationToken);
        return Ok(ApiResponse.OkPaged(items, meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var item = await _leads.GetAsync(societyId, id, cancellationToken);
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
        var updated = await _leads.UpdateAsync(societyId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        await _leads.DeleteAsync(societyId, id, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }

    [HttpGet("{id:guid}/activities")]
    public async Task<IActionResult> ListActivities(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var list = await _leads.ListActivitiesAsync(societyId, id, cancellationToken);
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
}
