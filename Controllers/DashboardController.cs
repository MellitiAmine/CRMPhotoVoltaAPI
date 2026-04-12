using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/dashboard")]
public sealed class DashboardController : TenantCrmControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(ITenantContext tenant, IDashboardService dashboard) : base(tenant)
    {
        _dashboard = dashboard;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var data = await _dashboard.GetOverviewAsync(RequireSociety(), cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis(CancellationToken cancellationToken)
    {
        var data = await _dashboard.GetKpisAsync(RequireSociety(), cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(CancellationToken cancellationToken)
    {
        var data = await _dashboard.GetRevenueAsync(RequireSociety(), cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }

    [HttpGet("pipeline")]
    public async Task<IActionResult> Pipeline(CancellationToken cancellationToken)
    {
        var data = await _dashboard.GetPipelineAsync(RequireSociety(), cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }

    [HttpGet("projects")]
    public async Task<IActionResult> Projects(CancellationToken cancellationToken)
    {
        var data = await _dashboard.GetProjectsAsync(RequireSociety(), cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }
}
