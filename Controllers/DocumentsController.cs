using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Documents;
using CrmPhotoVolta.Application.Storage;
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
    private readonly IFileStorageService _files;

    public DocumentsController(ITenantContext tenant, IDocumentService documents, IFileStorageService files) : base(tenant)
    {
        _documents = documents;
        _files = files;
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
        var folder = projectId is { } pid
            ? $"documents/projects/{pid:N}"
            : clientId is { } cid
                ? $"documents/clients/{cid:N}"
                : "documents/general";

        await using var stream = file.OpenReadStream();
        var stored = await _files.SaveAsync(new FileUploadInput
        {
            SocietyId        = societyId,
            RelativeFolder   = folder,
            OriginalFileName = file.FileName,
            ContentType      = file.ContentType,
            Length           = file.Length,
            Content          = stream,
            ImagesOnly       = false
        }, cancellationToken);

        var doc = await _documents.RegisterUploadAsync(
            societyId,
            projectId,
            clientId,
            type ?? "file",
            stored.PublicPath,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(new
        {
            doc.Id,
            doc.Type,
            Url = _files.ToAbsoluteUrl(stored.PublicPath),
            stored.StoredFileName,
            stored.ContentType,
            stored.SizeBytes,
            doc.UploadedAt
        }));
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
