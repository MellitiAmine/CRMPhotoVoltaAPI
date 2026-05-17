using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Calendar;
using CrmPhotoVolta.Application.Crm.Commercials;
using CrmPhotoVolta.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

/// <summary>
/// REST API surface for commercial (sales employee) profile management.
/// Base URL: /api/v1/commercials
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/commercials")]
public sealed class CommercialsController : TenantCrmControllerBase
{
    private readonly ICommercialService   _commercials;
    private readonly ICalendarQueryService _calendarQuery;
    private readonly ICurrentUser         _currentUser;

    public CommercialsController(
        ITenantContext tenant,
        ICommercialService commercials,
        ICalendarQueryService calendarQuery,
        ICurrentUser currentUser) : base(tenant)
    {
        _commercials    = commercials;
        _calendarQuery  = calendarQuery;
        _currentUser    = currentUser;
    }

    // ── GET /api/v1/commercials ──────────────────────────────────────────

    /// <summary>Returns a paged, filterable list of commercial profiles.</summary>
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
        var query = new CommercialListQuery(search, status, contractType, department, scoreTier, page, pageSize);
        var (items, pageMeta) = await _commercials.ListAsync(societyId, actorId, query, ct);
        var meta = new PaginationMeta
        {
            Page         = pageMeta.Page,
            PageSize     = pageMeta.PageSize,
            TotalItems   = pageMeta.TotalCount,
            TotalPages   = pageMeta.TotalPages,
            HasNext      = pageMeta.Page < pageMeta.TotalPages,
            HasPrevious  = pageMeta.Page > 1
        };
        return Ok(ApiResponse.OkPaged(items, meta));
    }

    // ── GET /api/v1/commercials/stats ────────────────────────────────────

    /// <summary>Returns aggregate KPI stats for the dashboard strip.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var stats = await _commercials.GetStatsAsync(societyId, actorId, ct);
        return Ok(ApiResponse.Ok(stats));
    }

    // ── GET /api/v1/commercials/{id} ─────────────────────────────────────

    /// <summary>Returns the full profile including attendance and emergency contact.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var profile = await _commercials.GetAsync(societyId, actorId, id, ct);
        return Ok(ApiResponse.Ok(profile));
    }

    // ── GET /api/v1/commercials/{id}/calendar-events ────────────────────

    /// <summary>
    /// Événements calendrier où ce commercial est invité (<c>Participants</c>) ou assigné (<c>AssignedToUserId</c>).
    /// Réutilise les mêmes règles d'accès que <see cref="Get"/>.
    /// </summary>
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

        var profile = await _commercials.GetAsync(societyId, actorId, id, ct);
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

    // ── POST /api/v1/commercials ─────────────────────────────────────────

    /// <summary>Creates a new commercial profile (HR onboarding).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCommercialRequest request,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);

        var created = await _commercials.CreateAsync(societyId, actorId, request, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    // ── PUT /api/v1/commercials/{id} ─────────────────────────────────────

    /// <summary>Updates HR fields of a commercial profile.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCommercialRequest request,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _commercials.UpdateAsync(societyId, actorId, id, request, ct);
        return Ok(ApiResponse.Ok(updated));
    }

    // ── PATCH /api/v1/commercials/{id}/kpis ─────────────────────────────

    /// <summary>
    /// Pushes a fresh KPI snapshot and triggers score recomputation.
    /// Typically called by a nightly sync job or after a lead/deal event.
    /// </summary>
    [HttpPatch("{id:guid}/kpis")]
    public async Task<IActionResult> UpdateKpis(
        Guid id,
        [FromBody] UpdateCommercialKpisRequest request,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _commercials.UpdateKpisAndScoreAsync(societyId, actorId, id, request, ct);
        return Ok(ApiResponse.Ok(updated));
    }

    // ── DELETE /api/v1/commercials/{id} ──────────────────────────────────

    /// <summary>Soft-deletes a commercial profile.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        await _commercials.DeleteAsync(societyId, actorId, id, ct);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }
}
