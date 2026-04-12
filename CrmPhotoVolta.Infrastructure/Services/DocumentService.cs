using CrmPhotoVolta.Application.Crm.Documents;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class DocumentService : IDocumentService
{
    private readonly AppDbContext _app;

    public DocumentService(AppDbContext app)
    {
        _app = app;
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

        return new DocumentDto
        {
            Id = doc.Id,
            ProjectId = doc.ProjectId,
            ClientId = doc.ClientId,
            Type = doc.Type,
            FileUrl = doc.FileUrl,
            UploadedAt = doc.UploadedAt
        };
    }

    public async Task<IReadOnlyList<DocumentDto>> ListByProjectAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        if (!await _app.Projects.AnyAsync(x => x.Id == projectId && x.SocietyId == societyId, cancellationToken))
            throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        return await _app.Documents.AsNoTracking()
            .Where(x => x.SocietyId == societyId && x.ProjectId == projectId)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new DocumentDto
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                ClientId = x.ClientId,
                Type = x.Type,
                FileUrl = x.FileUrl,
                UploadedAt = x.UploadedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentDto>> ListByClientAsync(Guid societyId, Guid clientId, CancellationToken cancellationToken = default)
    {
        if (!await _app.Clients.AnyAsync(x => x.Id == clientId && x.SocietyId == societyId, cancellationToken))
            throw new AppException("CLIENT_NOT_FOUND", "Client not found.", 404);

        return await _app.Documents.AsNoTracking()
            .Where(x => x.SocietyId == societyId && x.ClientId == clientId)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new DocumentDto
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                ClientId = x.ClientId,
                Type = x.Type,
                FileUrl = x.FileUrl,
                UploadedAt = x.UploadedAt
            })
            .ToListAsync(cancellationToken);
    }
}
