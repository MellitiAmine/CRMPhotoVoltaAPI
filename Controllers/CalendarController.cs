using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Calendar;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/calendar")]
public sealed class CalendarController : TenantCrmControllerBase
{
    private readonly ICalendarQueryService _query;
    private readonly ICalendarCommandService _command;
    private readonly ICurrentUser _currentUser;
    private readonly CoreDbContext _core;

    public CalendarController(
        ITenantContext tenant,
        ICalendarQueryService query,
        ICalendarCommandService command,
        ICurrentUser currentUser,
        CoreDbContext core) : base(tenant)
    {
        _query = query;
        _command = command;
        _currentUser = currentUser;
        _core = core;
    }

    // ── GET /api/v1/calendar ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] Guid? technicianId,
        [FromQuery] Guid? projectId,
        CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var (userId, isAdmin) = await GetCallerInfoAsync(societyId, cancellationToken);

        var list = await _query.ListAsync(societyId, from, to, technicianId, projectId, userId, isAdmin, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    // ── GET /api/v1/calendar/technicians/{id} ────────────────────────────────
    [HttpGet("technicians/{id:guid}")]
    public async Task<IActionResult> ByTechnician(Guid id, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var (userId, isAdmin) = await GetCallerInfoAsync(societyId, cancellationToken);

        var list = await _query.ListAsync(societyId, from, to, id, null, userId, isAdmin, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    // ── GET /api/v1/calendar/projects/{id} ───────────────────────────────────
    [HttpGet("projects/{id:guid}")]
    public async Task<IActionResult> ByProject(Guid id, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var (userId, isAdmin) = await GetCallerInfoAsync(societyId, cancellationToken);

        var list = await _query.ListAsync(societyId, from, to, null, id, userId, isAdmin, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    // ── POST /api/v1/calendar ─────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Policy = SocietyPolicies.Commercial)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCalendarEventRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var creatorId = _currentUser.UserId
            ?? throw new AppException("UNAUTHORIZED", "Authenticated user identity could not be resolved.", 401);

        var created = await _command.CreateAsync(societyId, creatorId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    // ── DELETE /api/v1/calendar/{id} ──────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = SocietyPolicies.Commercial)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var (userId, isAdmin) = await GetCallerInfoAsync(societyId, cancellationToken);

        if (userId is null)
            throw new AppException("UNAUTHORIZED", "Authenticated user identity could not be resolved.", 401);

        await _command.DeleteAsync(societyId, id, userId.Value, isAdmin, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Guid? userId, bool isAdmin)> GetCallerInfoAsync(Guid societyId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return (null, false);

        var isAdmin = await _core.UserSocieties
            .AsNoTracking()
            .Include(x => x.Role)
            .AnyAsync(x =>
                !x.IsDeleted &&
                x.UserId == userId.Value &&
                x.SocietyId == societyId &&
                x.Role != null &&
                x.Role.Name == "Admin",
            cancellationToken);

        return (userId, isAdmin);
    }
}
