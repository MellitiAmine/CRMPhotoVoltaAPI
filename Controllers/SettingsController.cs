using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/settings")]
public sealed class SettingsController : TenantCrmControllerBase
{
    private readonly ISocietySettingsService _settings;

    public SettingsController(ITenantContext tenant, ISocietySettingsService settings) : base(tenant)
    {
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var data = await _settings.GetAsync(RequireSociety(), cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] UpdateSocietySettingsRequest request, CancellationToken cancellationToken)
    {
        var data = await _settings.UpdateAsync(RequireSociety(), request, cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }
}
