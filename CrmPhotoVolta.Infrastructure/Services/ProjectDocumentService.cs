using CrmPhotoVolta.Application.Crm.Documents;
using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class ProjectDocumentService : IProjectDocumentService
{
    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;

    public ProjectDocumentService(AppDbContext app, CoreDbContext core)
    {
        _app = app;
        _core = core;
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

        return docs.Select(d => new ProjectDocumentDto
        {
            Id = d.Id,
            Type = d.Type,
            Name = d.Name,
            Url = d.Url,
            UploadedByUserId = d.UploadedByUserId,
            UploadedByName = d.UploadedByUserId.HasValue && names.TryGetValue(d.UploadedByUserId.Value, out var n) ? n : null,
            UploadedAt = d.UploadedAt
        }).ToList();
    }

    public async Task<ProjectDocumentDto> AddAsync(
        Guid societyId, Guid projectId, Guid uploaderUserId,
        UploadProjectDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _app.Projects.AnyAsync(p => p.Id == projectId && p.SocietyId == societyId, cancellationToken))
            throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Document name is required.", 400);

        if (string.IsNullOrWhiteSpace(request.Url))
            throw new AppException("VALIDATION_ERROR", "Document URL is required.", 400);

        var now = DateTimeOffset.UtcNow;
        var doc = new ProjectDocument
        {
            SocietyId = societyId,
            ProjectId = projectId,
            Type = request.Type,
            Name = request.Name.Trim(),
            Url = request.Url.Trim(),
            UploadedByUserId = uploaderUserId,
            UploadedAt = now,
            CreatedAt = now,
            CreatedById = uploaderUserId
        };
        _app.ProjectDocuments.Add(doc);

        _app.ProjectTimelineEvents.Add(new ProjectTimelineEvent
        {
            SocietyId = societyId,
            ProjectId = projectId,
            Type = ProjectTimelineEventType.DocumentUploaded,
            Description = $"Document «{doc.Name}» ({doc.Type}) ajouté.",
            CreatedByUserId = uploaderUserId,
            CreatedAt = now
        });

        await _app.SaveChangesAsync(cancellationToken);

        var uploaderName = await _core.Users.AsNoTracking()
            .Where(u => u.Id == uploaderUserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        return new ProjectDocumentDto
        {
            Id = doc.Id,
            Type = doc.Type,
            Name = doc.Name,
            Url = doc.Url,
            UploadedByUserId = uploaderUserId,
            UploadedByName = uploaderName,
            UploadedAt = now
        };
    }
}
