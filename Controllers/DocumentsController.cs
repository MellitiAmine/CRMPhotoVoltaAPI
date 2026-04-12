using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/documents")]
public sealed class DocumentsController : TenantCrmControllerBase
{
    private readonly IDocumentService _documents;
    private readonly IWebHostEnvironment _env;

    public DocumentsController(ITenantContext tenant, IDocumentService documents, IWebHostEnvironment env) : base(tenant)
    {
        _documents = documents;
        _env = env;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload(
        [FromForm] Guid? projectId,
        [FromForm] Guid? clientId,
        [FromForm] string? type,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return UnprocessableEntity(ApiResponse.Fail("VALIDATION_ERROR", "File is required.", null));

        var societyId = RequireSociety();
        var dir = Path.Combine(_env.ContentRootPath, "uploads", societyId.ToString("N"));
        Directory.CreateDirectory(dir);

        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        var fullPath = Path.Combine(dir, safeName);

        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream, cancellationToken);

        var publicUrl = $"/uploads/{societyId:N}/{safeName}";
        var doc = await _documents.RegisterUploadAsync(societyId, projectId, clientId, type ?? "file", publicUrl, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(doc));
    }

    [HttpGet("projects/{projectId:guid}")]
    public async Task<IActionResult> ByProject(Guid projectId, CancellationToken cancellationToken)
    {
        var list = await _documents.ListByProjectAsync(RequireSociety(), projectId, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpGet("clients/{clientId:guid}")]
    public async Task<IActionResult> ByClient(Guid clientId, CancellationToken cancellationToken)
    {
        var list = await _documents.ListByClientAsync(RequireSociety(), clientId, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }
}
