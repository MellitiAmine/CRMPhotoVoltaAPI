using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Contracts;
using CrmPhotoVolta.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1")]
public sealed class ContractsController : TenantCrmControllerBase
{
    private readonly IContractService _contracts;
    private readonly ICurrentUser _currentUser;

    public ContractsController(ITenantContext tenant, IContractService contracts, ICurrentUser currentUser)
        : base(tenant)
    {
        _contracts = contracts;
        _currentUser = currentUser;
    }

    [HttpGet("projects/{projectId:guid}/contracts")]
    public async Task<IActionResult> ListByProject(Guid projectId, CancellationToken cancellationToken)
    {
        var list = await _contracts.ListByProjectAsync(RequireSociety(), projectId, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpGet("contracts/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _contracts.GetAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    [HttpPost("contracts")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateContractRequest request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var created = await _contracts.CreateAsync(RequireSociety(), actorId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("contracts/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContractRequest request, CancellationToken cancellationToken)
    {
        var updated = await _contracts.UpdateAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }
}
