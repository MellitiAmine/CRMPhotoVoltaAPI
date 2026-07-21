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
    private readonly ICommercialService        _commercials;
    private readonly ICommercialTimeEntryService _timeEntries;
    private readonly ICalendarQueryService     _calendarQuery;
    private readonly ICurrentUser                _currentUser;

    public CommercialsController(
        ITenantContext tenant,
        ICommercialService commercials,
        ICommercialTimeEntryService timeEntries,
        ICalendarQueryService calendarQuery,
        ICurrentUser currentUser) : base(tenant)
    {
        _commercials   = commercials;
        _timeEntries   = timeEntries;
        _calendarQuery = calendarQuery;
        _currentUser   = currentUser;
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

    // ── GET /api/v1/commercials/me ───────────────────────────────────────

    /// <summary>Profile of the authenticated commercial user (self-service pointage).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var profile = await _commercials.GetMeAsync(societyId, actorId, ct);
        return Ok(ApiResponse.Ok(profile));
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

    // ── PATCH /api/v1/commercials/{id}/attendance ─────────────────────────

    /// <summary>Updates the current-month attendance snapshot.</summary>
    [HttpPatch("{id:guid}/attendance")]
    public async Task<IActionResult> UpdateAttendance(
        Guid id,
        [FromBody] UpdateCommercialAttendanceRequest request,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _commercials.UpdateAttendanceAsync(societyId, actorId, id, request, ct);
        return Ok(ApiResponse.Ok(updated));
    }

    // ── POST /api/v1/commercials/{id}/sync-kpis ───────────────────────────

    /// <summary>Recomputes KPIs from CRM data (leads, quotes, calendar).</summary>
    [HttpPost("{id:guid}/sync-kpis")]
    public async Task<IActionResult> SyncKpis(Guid id, CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _commercials.SyncKpisAsync(societyId, actorId, id, ct);
        return Ok(ApiResponse.Ok(updated));
    }

    // ── PATCH /api/v1/commercials/{id}/account ────────────────────────────

    /// <summary>Updates login email and/or password for the linked CRM user.</summary>
    [HttpPatch("{id:guid}/account")]
    public async Task<IActionResult> UpdateAccount(
        Guid id,
        [FromBody] UpdateCommercialAccountRequest request,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        await _commercials.UpdateAccountAsync(societyId, actorId, id, request, ct);
        return Ok(ApiResponse.Ok(new { updated = true }));
    }

    // ── Time entries (pointage) ───────────────────────────────────────────

    [HttpGet("{id:guid}/time-entries")]
    public async Task<IActionResult> GetTimeMonth(
        Guid id,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var bundle = await _timeEntries.GetMonthAsync(societyId, actorId, id, year, month, ct);
        return Ok(ApiResponse.Ok(bundle));
    }

    [HttpPost("{id:guid}/time-entries")]
    public async Task<IActionResult> CreateTimeEntry(
        Guid id,
        [FromBody] CreateCommercialTimeEntryRequest request,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var created = await _timeEntries.CreateAsync(societyId, actorId, id, request, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}/time-entries/{entryId:guid}")]
    public async Task<IActionResult> UpdateTimeEntry(
        Guid id,
        Guid entryId,
        [FromBody] UpdateCommercialTimeEntryRequest request,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _timeEntries.UpdateAsync(societyId, actorId, id, entryId, request, ct);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}/time-entries/{entryId:guid}")]
    public async Task<IActionResult> DeleteTimeEntry(Guid id, Guid entryId, CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        await _timeEntries.DeleteAsync(societyId, actorId, id, entryId, ct);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }

    [HttpPost("{id:guid}/attendance/recompute")]
    public async Task<IActionResult> RecomputeAttendance(
        Guid id,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct = default)
    {
        var societyId = RequireSociety();
        var actorId   = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var summary = await _timeEntries.RecomputeMonthAsync(societyId, actorId, id, year, month, ct);
        return Ok(ApiResponse.Ok(summary));
    }
}
