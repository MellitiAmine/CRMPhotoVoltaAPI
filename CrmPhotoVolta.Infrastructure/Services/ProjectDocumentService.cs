using CrmPhotoVolta.Application.Crm.Documents;
using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Storage;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class ProjectDocumentService : IProjectDocumentService
{
    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;
    private readonly IFileStorageService _files;

    public ProjectDocumentService(AppDbContext app, CoreDbContext core, IFileStorageService files)
    {
        _app = app;
        _core = core;
        _files = files;
    }

    public async Task<IReadOnlyList<ProjectDocumentDto>> ListAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var docs = await _app.ProjectDocuments.AsNoTracking()
            .Where(d => d.ProjectId == projectId && d.SocietyId == societyId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

        var userIds = docs.Where(d => d.UploadedByUserId.HasValue)
            .Select(d => d.UploadedByUserId!.Value).Distinct().ToList();

        var names = userIds.Any()
            ? await _core.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken)
            : new Dictionary<Guid, string>();

        return docs.Select(d => MapDto(d, names)).ToList();
    }

    public async Task<ProjectDocumentDto> UploadAsync(
        Guid societyId,
        Guid projectId,
        Guid uploaderUserId,
        ProjectDocumentType type,
        string? displayName,
        string fileName,
        string contentType,
        long length,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (!await _app.Projects.AnyAsync(p => p.Id == projectId && p.SocietyId == societyId, cancellationToken))
            throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        var stored = await _files.SaveAsync(new FileUploadInput
        {
            SocietyId      = societyId,
            RelativeFolder = $"projects/{projectId:N}/documents",
            OriginalFileName = fileName,
            ContentType    = contentType,
            Length         = length,
            Content        = content,
            ImagesOnly     = false
        }, cancellationToken);

        var name = string.IsNullOrWhiteSpace(displayName)
            ? Path.GetFileNameWithoutExtension(fileName)
            : displayName.Trim();

        var now = DateTimeOffset.UtcNow;
        var doc = new ProjectDocument
        {
            SocietyId        = societyId,
            ProjectId        = projectId,
            Type             = type,
            Name             = name,
            Url              = stored.PublicPath,
            UploadedByUserId = uploaderUserId,
            UploadedAt       = now,
            CreatedAt        = now,
            CreatedById      = uploaderUserId
        };
        _app.ProjectDocuments.Add(doc);

        _app.ProjectTimelineEvents.Add(new ProjectTimelineEvent
        {
            SocietyId       = societyId,
            ProjectId       = projectId,
            Type            = ProjectTimelineEventType.DocumentUploaded,
            Description     = $"Document «{doc.Name}» ({doc.Type}) ajouté.",
            CreatedByUserId = uploaderUserId,
            CreatedAt       = now
        });

        await _app.SaveChangesAsync(cancellationToken);

        var uploaderName = await _core.Users.AsNoTracking()
            .Where(u => u.Id == uploaderUserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        return MapDto(doc, uploaderName, stored);
    }

    public async Task DeleteAsync(
        Guid societyId, Guid projectId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var doc = await _app.ProjectDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.ProjectId == projectId && d.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("DOCUMENT_NOT_FOUND", "Project document not found.", 404);

        await _files.DeleteByPublicPathAsync(doc.Url, cancellationToken);
        doc.IsDeleted = true;
        doc.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }

    private ProjectDocumentDto MapDto(
        ProjectDocument d,
        IReadOnlyDictionary<Guid, string> names,
        StoredFileResult? stored = null) =>
        MapDto(d, d.UploadedByUserId.HasValue && names.TryGetValue(d.UploadedByUserId.Value, out var n) ? n : null, stored);

    private ProjectDocumentDto MapDto(ProjectDocument d, string? uploaderName, StoredFileResult? stored = null) => new()
    {
        Id               = d.Id,
        Type             = d.Type,
        Name             = d.Name,
        Url              = _files.ToAbsoluteUrl(d.Url),
        FileName         = stored?.StoredFileName ?? Path.GetFileName(d.Url),
        ContentType      = stored?.ContentType ?? "application/octet-stream",
        SizeBytes        = stored?.SizeBytes ?? 0,
        UploadedByUserId = d.UploadedByUserId,
        UploadedByName   = uploaderName,
        UploadedAt       = d.UploadedAt
    };
}
