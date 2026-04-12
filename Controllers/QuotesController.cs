using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Quotes;
using CrmPhotoVolta.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/quotes")]
public sealed class QuotesController : TenantCrmControllerBase
{
    private readonly IQuoteService _quotes;
    private readonly ICurrentUser _currentUser;

    public QuotesController(ITenantContext tenant, IQuoteService quotes, ICurrentUser currentUser) : base(tenant)
    {
        _quotes = quotes;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        var (items, meta) = await _quotes.ListPagedAsync(RequireSociety(), query.ToRequest(), cancellationToken);
        return Ok(ApiResponse.OkPaged(items, meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _quotes.GetAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateQuoteRequest request, CancellationToken cancellationToken)
    {
        var created = await _quotes.CreateAsync(RequireSociety(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuoteRequest request, CancellationToken cancellationToken)
    {
        var updated = await _quotes.UpdateAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _quotes.DeleteAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _quotes.SendAsync(RequireSociety(), id, actorId, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
    {
        var updated = await _quotes.AcceptAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        var updated = await _quotes.RejectAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPost("{id:guid}/convert-to-project")]
    public async Task<IActionResult> ConvertToProject(Guid id, [FromBody] ConvertQuoteToProjectRequest request, CancellationToken cancellationToken)
    {
        var updated = await _quotes.ConvertToProjectAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }
}
