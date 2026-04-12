using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Calendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/calendar")]
public sealed class CalendarController : TenantCrmControllerBase
{
    private readonly ICalendarQueryService _calendar;

    public CalendarController(ITenantContext tenant, ICalendarQueryService calendar) : base(tenant)
    {
        _calendar = calendar;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] Guid? technicianId,
        [FromQuery] Guid? projectId,
        CancellationToken cancellationToken)
    {
        var list = await _calendar.ListAsync(RequireSociety(), from, to, technicianId, projectId, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpGet("technicians/{id:guid}")]
    public async Task<IActionResult> ByTechnician(Guid id, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var list = await _calendar.ListAsync(RequireSociety(), from, to, id, null, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpGet("projects/{id:guid}")]
    public async Task<IActionResult> ByProject(Guid id, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var list = await _calendar.ListAsync(RequireSociety(), from, to, null, id, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }
}
