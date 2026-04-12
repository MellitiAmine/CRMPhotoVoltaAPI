using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/roles/catalog")]
public sealed class RolesCatalogController : ControllerBase
{
    [HttpGet("commercial-suggestions")]
    public IActionResult CommercialSuggestions()
    {
        return Ok(ApiResponse.Ok(CommercialRoleCatalog.SuggestedNames));
    }
}
