using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Items;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/items")]
public sealed class ItemsController : TenantCrmControllerBase
{
    private readonly IItemService _items;

    public ItemsController(ITenantContext tenant, IItemService items) : base(tenant)
    {
        _items = items;
    }

    /// <summary>List catalog items. Optional <c>societyId</c> must match JWT tenant.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? societyId, CancellationToken cancellationToken)
    {
        var society = ResolveSocietyFromOptionalQuery(societyId);
        var list = await _items.ListAsync(society, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateItemRequest request,
        [FromQuery] Guid? societyId,
        CancellationToken cancellationToken)
    {
        var society = ResolveSocietyFromOptionalQuery(societyId);
        var created = await _items.CreateAsync(society, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }
}
