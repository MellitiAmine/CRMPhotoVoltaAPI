using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/clients")]
public sealed class ClientsController : TenantCrmControllerBase
{
    private readonly IClientService _clients;

    public ClientsController(ITenantContext tenant, IClientService clients) : base(tenant)
    {
        _clients = clients;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search = null,
        [FromQuery] string? activity = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortOrder = "desc",
        CancellationToken cancellationToken = default)
    {
        var societyId = RequireSociety();
        var query = new ClientListQuery(search, activity, page, pageSize, sortBy, sortOrder);
        var (items, meta) = await _clients.ListPagedAsync(societyId, query, cancellationToken);
        return Ok(ApiResponse.OkPaged(items, meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var item = await _clients.GetAsync(societyId, id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    /// <summary>Vue 360° client : projets, factures, installations, paiements.</summary>
    [HttpGet("{id:guid}/360")]
    public async Task<IActionResult> Get360(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var detail = await _clients.Get360Async(societyId, id, cancellationToken);
        return Ok(ApiResponse.Ok(detail));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var created = await _clients.CreateAsync(societyId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest request, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        var updated = await _clients.UpdateAsync(societyId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var societyId = RequireSociety();
        await _clients.DeleteAsync(societyId, id, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }
}
