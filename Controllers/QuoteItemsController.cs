using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Quotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/quote-items")]
public sealed class QuoteItemsController : TenantCrmControllerBase
{
    private readonly IQuoteItemLineService _lines;

    public QuoteItemsController(ITenantContext tenant, IQuoteItemLineService lines) : base(tenant)
    {
        _lines = lines;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateQuoteItemLineRequest request,
        [FromQuery] Guid? societyId,
        CancellationToken cancellationToken)
    {
        var society = ResolveSocietyFromOptionalQuery(societyId);
        var quote = await _lines.AddLineAsync(society, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(quote));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateQuoteItemLineRequest request,
        [FromQuery] Guid? societyId,
        CancellationToken cancellationToken)
    {
        var society = ResolveSocietyFromOptionalQuery(societyId);
        var quote = await _lines.UpdateLineAsync(society, id, request, cancellationToken);
        return Ok(ApiResponse.Ok(quote));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] Guid? societyId,
        CancellationToken cancellationToken)
    {
        var society = ResolveSocietyFromOptionalQuery(societyId);
        var quote = await _lines.DeleteLineAsync(society, id, cancellationToken);
        return Ok(ApiResponse.Ok(quote));
    }
}
