using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Invoices;
using CrmPhotoVolta.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/invoices")]
public sealed class InvoicesController : TenantCrmControllerBase
{
    private readonly IInvoiceService _invoices;
    private readonly ICurrentUser _currentUser;

    public InvoicesController(ITenantContext tenant, IInvoiceService invoices, ICurrentUser currentUser)
        : base(tenant)
    {
        _invoices = invoices;
        _currentUser = currentUser;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _invoices.GetAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var created = await _invoices.CreateAsync(RequireSociety(), actorId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var updated = await _invoices.UpdateAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPost("{id:guid}/payments")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddPayment(Guid id, [FromBody] AddPaymentRequest request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _invoices.AddPaymentAsync(RequireSociety(), id, actorId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(updated));
    }
}
