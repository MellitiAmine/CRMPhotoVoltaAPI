using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Calendar;
using CrmPhotoVolta.Application.Crm.Techniciens;
using CrmPhotoVolta.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

/// <summary>
/// REST API surface for technicien (field technician) profile management.
/// Base URL: /api/v1/techniciens
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/techniciens")]
public sealed class TechniciensController : TenantCrmControllerBase
{
    private readonly ITechnicienService    _techniciens;
    private readonly ICalendarQueryService _calendarQuery;
    private readonly ICurrentUser          _currentUser;

    public TechniciensController(
        ITenantContext tenant,
        ITechnicienService techniciens,
        ICalendarQueryService calendarQuery,
        ICurrentUser currentUser) : base(tenant)
    {
        _techniciens   = techniciens;
        _calendarQuery = calendarQuery;
        _currentUser   = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search       = null,
        [FromQuery] string? status       = null,
        [FromQuery] string? contractType = null,
        [FromQuery] string? department   = null,
        [FromQuery] string? scoreTier    = null,
        [FromQuery] int     page         = 1,
        [FromQuery] int     pageSize     = 20,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var query = new TechnicienListQuery(search, status, contractType, department, scoreTier, page, pageSize);
        var (items, pageMeta) = await _techniciens.ListAsync(societyId, actorId, query, ct);
        var meta = new PaginationMeta
        {
            Page        = pageMeta.Page,
            PageSize    = pageMeta.PageSize,
            TotalItems  = pageMeta.TotalCount,
            TotalPages  = pageMeta.TotalPages,
            HasNext     = pageMeta.Page < pageMeta.TotalPages,
            HasPrevious = pageMeta.Page > 1
        };
        return Ok(ApiResponse.OkPaged(items, meta));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var stats = await _techniciens.GetStatsAsync(societyId, actorId, ct);
        return Ok(ApiResponse.Ok(stats));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var profile = await _techniciens.GetAsync(societyId, actorId, id, ct);
        return Ok(ApiResponse.Ok(profile));
    }

    [HttpGet("{id:guid}/calendar-events")]
    public async Task<IActionResult> CalendarEvents(
        Guid id,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);

        var profile = await _techniciens.GetAsync(societyId, actorId, id, ct);
        if (profile.UserId is null)
            return Ok(ApiResponse.Ok(Array.Empty<CalendarEventDto>()));

        var events = await _calendarQuery.ListEventsForSocietyUserAsync(
            societyId,
            profile.UserId.Value,
            from,
            to,
            ct);

        return Ok(ApiResponse.Ok(events));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTechnicienRequest request,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);

        var created = await _techniciens.CreateAsync(societyId, actorId, request, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTechnicienRequest request,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _techniciens.UpdateAsync(societyId, actorId, id, request, ct);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPatch("{id:guid}/kpis")]
    public async Task<IActionResult> UpdateKpis(
        Guid id,
        [FromBody] UpdateTechnicienKpisRequest request,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _techniciens.UpdateKpisAndScoreAsync(societyId, actorId, id, request, ct);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        await _techniciens.DeleteAsync(societyId, actorId, id, ct);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }
}
