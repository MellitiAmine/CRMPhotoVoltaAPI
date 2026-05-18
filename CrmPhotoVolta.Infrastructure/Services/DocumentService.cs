using CrmPhotoVolta.Application.Crm.Documents;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Storage;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class DocumentService : IDocumentService
{
    private readonly AppDbContext _app;
    private readonly IFileStorageService _files;

    public DocumentService(AppDbContext app, IFileStorageService files)
    {
        _app = app;
        _files = files;
    }

    public async Task<DocumentDto> RegisterUploadAsync(
        Guid societyId,
        Guid? projectId,
        Guid? clientId,
        string type,
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new AppException("VALIDATION_ERROR", "FileUrl is required.", 400);

        if (projectId is { } pid &&
            !await _app.Projects.AnyAsync(x => x.Id == pid && x.SocietyId == societyId, cancellationToken))
            throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        if (clientId is { } cid &&
            !await _app.Clients.AnyAsync(x => x.Id == cid && x.SocietyId == societyId, cancellationToken))
            throw new AppException("CLIENT_NOT_FOUND", "Client not found.", 404);

        var doc = new Document
        {
            SocietyId = societyId,
            ProjectId = projectId,
            ClientId = clientId,
            Type = string.IsNullOrWhiteSpace(type) ? "file" : type.Trim(),
            FileUrl = fileUrl.Trim(),
            UploadedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.Documents.Add(doc);
        await _app.SaveChangesAsync(cancellationToken);

        return MapDto(doc);
    }

    public async Task<IReadOnlyList<DocumentDto>> ListByProjectAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        if (!await _app.Projects.AnyAsync(x => x.Id == projectId && x.SocietyId == societyId, cancellationToken))
            throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        var rows = await _app.Documents.AsNoTracking()
            .Where(x => x.SocietyId == societyId && x.ProjectId == projectId)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(MapDto).ToList();
    }

    public async Task<IReadOnlyList<DocumentDto>> ListByClientAsync(Guid societyId, Guid clientId, CancellationToken cancellationToken = default)
    {
        if (!await _app.Clients.AnyAsync(x => x.Id == clientId && x.SocietyId == societyId, cancellationToken))
            throw new AppException("CLIENT_NOT_FOUND", "Client not found.", 404);

        var rows = await _app.Documents.AsNoTracking()
            .Where(x => x.SocietyId == societyId && x.ClientId == clientId)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(MapDto).ToList();
    }

    private DocumentDto MapDto(Document doc) => new()
    {
        Id         = doc.Id,
        ProjectId  = doc.ProjectId,
        ClientId   = doc.ClientId,
        Type       = doc.Type,
        FileUrl    = _files.ToAbsoluteUrl(doc.FileUrl),
        UploadedAt = doc.UploadedAt
    };
}
