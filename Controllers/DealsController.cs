using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Deals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/deals")]
public sealed class DealsController : TenantCrmControllerBase
{
    private readonly IDealService _deals;

    public DealsController(ITenantContext tenant, IDealService deals) : base(tenant)
    {
        _deals = deals;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var (items, meta) = await _deals.ListPagedAsync(societyId, query.ToRequest(), cancellationToken);
        return Ok(ApiResponse.OkPaged(items, meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var item = await _deals.GetAsync(societyId, id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateDealRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var created = await _deals.CreateAsync(societyId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDealRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var updated = await _deals.UpdateAsync(societyId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        await _deals.DeleteAsync(societyId, id, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }
}
