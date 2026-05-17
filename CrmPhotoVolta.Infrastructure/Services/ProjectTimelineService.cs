using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class ProjectTimelineService : IProjectTimelineService
{
    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;

    public ProjectTimelineService(AppDbContext app, CoreDbContext core)
    {
        _app = app;
        _core = core;
    }

    public async Task<IReadOnlyList<ProjectTimelineEventDto>> GetTimelineAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        if (!await _app.Projects.AnyAsync(p => p.Id == projectId && p.SocietyId == societyId, cancellationToken))
            throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        var events = await _app.ProjectTimelineEvents.AsNoTracking()
            .Where(e => e.ProjectId == projectId && e.SocietyId == societyId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var userIds = events.Where(e => e.CreatedByUserId.HasValue)
            .Select(e => e.CreatedByUserId!.Value).Distinct().ToList();

        var names = userIds.Any()
            ? await _core.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken)
            : new Dictionary<Guid, string>();

        return events.Select(e => new ProjectTimelineEventDto
        {
            Id = e.Id,
            Type = e.Type,
            Description = e.Description,
            CreatedByUserId = e.CreatedByUserId,
            CreatedByName = e.CreatedByUserId.HasValue && names.TryGetValue(e.CreatedByUserId.Value, out var n) ? n : null,
            CreatedAt = e.CreatedAt
        }).ToList();
    }

    public async Task<ProjectTimelineEventDto> AddEventAsync(
        Guid societyId, Guid projectId, Guid actorUserId,
        AddTimelineEventRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _app.Projects.AnyAsync(p => p.Id == projectId && p.SocietyId == societyId, cancellationToken))
            throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new AppException("VALIDATION_ERROR", "Description is required.", 400);

        var now = DateTimeOffset.UtcNow;
        var ev = new ProjectTimelineEvent
        {
            SocietyId = societyId,
            ProjectId = projectId,
            Type = request.Type,
            Description = request.Description.Trim(),
            CreatedByUserId = actorUserId,
            CreatedAt = now
        };
        _app.ProjectTimelineEvents.Add(ev);
        await _app.SaveChangesAsync(cancellationToken);

        var actorName = await _core.Users.AsNoTracking()
            .Where(u => u.Id == actorUserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        return new ProjectTimelineEventDto
        {
            Id = ev.Id,
            Type = ev.Type,
            Description = ev.Description,
            CreatedByUserId = actorUserId,
            CreatedByName = actorName,
            CreatedAt = now
        };
    }
}
