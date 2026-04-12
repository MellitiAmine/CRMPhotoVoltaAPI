using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/reports")]
public sealed class ReportsController : TenantCrmControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(ITenantContext tenant, IReportService reports) : base(tenant)
    {
        _reports = reports;
    }

    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var data = await _reports.GetSalesAsync(RequireSociety(), from, to, cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }

    [HttpGet("projects")]
    public async Task<IActionResult> Projects(CancellationToken cancellationToken)
    {
        var data = await _reports.GetProjectsAsync(RequireSociety(), cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }

    [HttpGet("technicians")]
    public async Task<IActionResult> Technicians(CancellationToken cancellationToken)
    {
        var data = await _reports.GetTechniciansAsync(RequireSociety(), cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }

    [HttpGet("conversion")]
    public async Task<IActionResult> Conversion(CancellationToken cancellationToken)
    {
        var data = await _reports.GetConversionAsync(RequireSociety(), cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }
}
