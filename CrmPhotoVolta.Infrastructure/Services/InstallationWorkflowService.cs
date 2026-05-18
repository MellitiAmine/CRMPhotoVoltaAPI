using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Installations;
using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Storage;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class InstallationWorkflowService : IInstallationWorkflowService
{
    private static readonly string[] DefaultChecklist =
    [
        "Préparation chantier",
        "Pose structure / rails",
        "Pose panneaux",
        "Câblage DC / AC",
        "Mise en service",
        "Contrôle qualité / photos"
    ];

    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;
    private readonly IFileStorageService _files;

    public InstallationWorkflowService(AppDbContext app, CoreDbContext core, IFileStorageService files)
    {
        _app = app;
        _core = core;
        _files = files;
    }

    public async Task<(IReadOnlyList<InstallationListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        Guid? projectId,
        Guid? technicianId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _app.Installations.AsNoTracking()
            .Include(x => x.Project).ThenInclude(p => p.Client)
            .Include(x => x.Checklist)
            .Where(x => x.SocietyId == societyId);

        if (projectId is { } pid)
            query = query.Where(x => x.ProjectId == pid);

        if (technicianId is { } tid)
            query = query.Where(x => x.TechnicianId == tid);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var s = pagination.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Project.Name.ToLower().Contains(s) ||
                (x.Project.Reference != null && x.Project.Reference.ToLower().Contains(s)) ||
                x.Project.Client!.Name.ToLower().Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);

        query = pagination.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.Date)
            : query.OrderByDescending(x => x.Date);

        var rows = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var techNames = await ProjectUserNameResolver.LoadNamesAsync(
            _core, rows.Select(x => (Guid?)x.TechnicianId), cancellationToken);

        var items = rows.Select(x => MapListItem(x, ProjectUserNameResolver.Resolve(techNames, x.TechnicianId))).ToList();
        return (items, pagination.ToMeta(total));
    }

    public async Task<IReadOnlyList<InstallationListItemDto>> ListByProjectAsync(
        Guid societyId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await EnsureProjectAsync(societyId, projectId, cancellationToken);

        var rows = await _app.Installations.AsNoTracking()
            .Include(x => x.Project).ThenInclude(p => p.Client)
            .Include(x => x.Checklist)
            .Where(x => x.SocietyId == societyId && x.ProjectId == projectId)
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);

        var techNames = await ProjectUserNameResolver.LoadNamesAsync(
            _core, rows.Select(x => (Guid?)x.TechnicianId), cancellationToken);

        return rows.Select(x => MapListItem(x, ProjectUserNameResolver.Resolve(techNames, x.TechnicianId))).ToList();
    }

    public async Task<InstallationDto> GetAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken = default)
    {
        var inst = await LoadInstallationAsync(societyId, installationId, cancellationToken);
        TenantGuard.EnsureSameTenant(inst, societyId);
        return await MapDetailAsync(inst, cancellationToken);
    }

    public async Task<InstallationDto> CreateAsync(
        Guid societyId,
        CreateInstallationRequest request,
        CancellationToken cancellationToken = default)
    {
        var project = await _app.Projects.AsNoTracking()
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        await EnsureUserInSocietyAsync(societyId, request.TechnicianId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var inst = new Installation
        {
            SocietyId = societyId,
            ProjectId = request.ProjectId,
            TechnicianId = request.TechnicianId,
            Date = request.Date,
            Status = InstallationStatus.Scheduled,
            CreatedAt = now
        };

        var checklistLabels = request.DefaultChecklistItems.Count > 0
            ? request.DefaultChecklistItems
            : DefaultChecklist;

        foreach (var label in checklistLabels.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            inst.Checklist.Add(new InstallationChecklistItem
            {
                SocietyId = societyId,
                Item = label.Trim(),
                IsCompleted = false,
                CreatedAt = now
            });
        }

        _app.Installations.Add(inst);

        _app.ProjectTimelineEvents.Add(new ProjectTimelineEvent
        {
            SocietyId = societyId,
            ProjectId = request.ProjectId,
            Type = ProjectTimelineEventType.InstallationPlanned,
            Description = $"Installation planifiée le {request.Date:dd/MM/yyyy}.",
            CreatedAt = now
        });

        await _app.SaveChangesAsync(cancellationToken);

        return await GetAsync(societyId, inst.Id, cancellationToken);
    }

    public async Task<InstallationDto> UpdateAsync(
        Guid societyId,
        Guid installationId,
        UpdateInstallationRequest request,
        CancellationToken cancellationToken = default)
    {
        var inst = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);
        await EnsureUserInSocietyAsync(societyId, request.TechnicianId, cancellationToken);

        inst.TechnicianId = request.TechnicianId;
        inst.Date = request.Date;
        if (request.Status is { } status)
            inst.Status = status;
        inst.UpdatedAt = DateTimeOffset.UtcNow;

        await _app.SaveChangesAsync(cancellationToken);
        return await GetAsync(societyId, installationId, cancellationToken);
    }

    public async Task<InstallationDto> StartAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken = default)
    {
        var inst = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);
        TenantGuard.EnsureSameTenant(inst, societyId);

        var now = DateTimeOffset.UtcNow;
        inst.Status = InstallationStatus.InProgress;
        inst.UpdatedAt = now;

        _app.ProjectTimelineEvents.Add(new ProjectTimelineEvent
        {
            SocietyId = societyId,
            ProjectId = inst.ProjectId,
            Type = ProjectTimelineEventType.InstallationStarted,
            Description = "Installation démarrée sur site.",
            CreatedAt = now
        });

        await _app.SaveChangesAsync(cancellationToken);
        return await GetAsync(societyId, installationId, cancellationToken);
    }

    public async Task<InstallationDto> CompleteAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken = default)
    {
        var inst = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);
        TenantGuard.EnsureSameTenant(inst, societyId);

        var now = DateTimeOffset.UtcNow;
        inst.Status = InstallationStatus.Completed;
        inst.UpdatedAt = now;

        var project = await _app.Projects
            .FirstOrDefaultAsync(p => p.Id == inst.ProjectId && p.SocietyId == societyId, cancellationToken);

        if (project is not null)
        {
            if (project.Status == ProjectStatus.Installation || project.Status == ProjectStatus.Planning)
                project.Status = ProjectStatus.Activated;

            project.ProgressPercent = Math.Max(project.ProgressPercent, 90);
            project.LastActivityAt = now;
            project.UpdatedAt = now;
        }

        _app.ProjectTimelineEvents.Add(new ProjectTimelineEvent
        {
            SocietyId = societyId,
            ProjectId = inst.ProjectId,
            Type = ProjectTimelineEventType.InstallationCompleted,
            Description = "Installation terminée.",
            CreatedAt = now
        });

        await _app.SaveChangesAsync(cancellationToken);
        return await GetAsync(societyId, installationId, cancellationToken);
    }

    public async Task<IReadOnlyList<InstallationChecklistItemDto>> ListChecklistAsync(
        Guid societyId,
        Guid installationId,
        CancellationToken cancellationToken = default)
    {
        _ = await LoadInstallationAsync(societyId, installationId, cancellationToken);
        return await QueryChecklistDtosAsync(societyId, installationId, cancellationToken);
    }

    public async Task<InstallationChecklistItemDto> CreateChecklistItemAsync(
        Guid societyId,
        Guid installationId,
        CreateInstallationChecklistItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Item))
            throw new AppException("VALIDATION_ERROR", "Item label is required.", 400);

        _ = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var row = new InstallationChecklistItem
        {
            SocietyId       = societyId,
            InstallationId  = installationId,
            Item            = request.Item.Trim(),
            IsCompleted     = false,
            CreatedAt       = now
        };

        _app.InstallationChecklistItems.Add(row);
        await _app.SaveChangesAsync(cancellationToken);

        return new InstallationChecklistItemDto
        {
            Id          = row.Id,
            Item        = row.Item,
            IsCompleted = row.IsCompleted
        };
    }

    public async Task<IReadOnlyList<InstallationChecklistItemDto>> UpdateChecklistAsync(
        Guid societyId,
        Guid installationId,
        UpdateInstallationChecklistRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);

        foreach (var u in request.Items)
        {
            var row = await _app.InstallationChecklistItems
                .FirstOrDefaultAsync(x => x.Id == u.Id && x.InstallationId == installationId && x.SocietyId == societyId, cancellationToken);
            if (row is null)
                continue;

            row.IsCompleted = u.IsCompleted;
            if (u.Item is { } label && !string.IsNullOrWhiteSpace(label))
                row.Item = label.Trim();
            row.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _app.SaveChangesAsync(cancellationToken);
        return await QueryChecklistDtosAsync(societyId, installationId, cancellationToken);
    }

    public async Task<InstallationChecklistItemDto> UpdateChecklistItemAsync(
        Guid societyId,
        Guid installationId,
        Guid itemId,
        UpdateInstallationChecklistItemRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);

        var row = await _app.InstallationChecklistItems
            .FirstOrDefaultAsync(x => x.Id == itemId && x.InstallationId == installationId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("CHECKLIST_ITEM_NOT_FOUND", "Checklist item not found.", 404);

        if (request.Item is { } label)
        {
            if (string.IsNullOrWhiteSpace(label))
                throw new AppException("VALIDATION_ERROR", "Item label cannot be empty.", 400);
            row.Item = label.Trim();
        }

        if (request.IsCompleted is { } completed)
            row.IsCompleted = completed;

        row.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);

        return new InstallationChecklistItemDto
        {
            Id          = row.Id,
            Item        = row.Item,
            IsCompleted = row.IsCompleted
        };
    }

    public async Task DeleteChecklistItemAsync(
        Guid societyId,
        Guid installationId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        _ = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);

        var row = await _app.InstallationChecklistItems
            .FirstOrDefaultAsync(x => x.Id == itemId && x.InstallationId == installationId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("CHECKLIST_ITEM_NOT_FOUND", "Checklist item not found.", 404);

        row.IsDeleted = true;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InstallationChecklistItemDto>> InitializeChecklistAsync(
        Guid societyId,
        Guid installationId,
        InitializeInstallationChecklistRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var inst = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);

        var existingCount = await _app.InstallationChecklistItems
            .CountAsync(x => x.InstallationId == installationId && x.SocietyId == societyId, cancellationToken);

        if (existingCount > 0)
            return await QueryChecklistDtosAsync(societyId, installationId, cancellationToken);

        var labels = request?.Items is { Count: > 0 } custom
            ? custom.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList()
            : DefaultChecklist.ToList();

        var now = DateTimeOffset.UtcNow;
        foreach (var label in labels)
        {
            inst.Checklist.Add(new InstallationChecklistItem
            {
                SocietyId      = societyId,
                Item           = label,
                IsCompleted    = false,
                CreatedAt      = now
            });
        }

        await _app.SaveChangesAsync(cancellationToken);
        return await QueryChecklistDtosAsync(societyId, installationId, cancellationToken);
    }

    private async Task<IReadOnlyList<InstallationChecklistItemDto>> QueryChecklistDtosAsync(
        Guid societyId,
        Guid installationId,
        CancellationToken cancellationToken) =>
        await _app.InstallationChecklistItems.AsNoTracking()
            .Where(x => x.InstallationId == installationId && x.SocietyId == societyId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Item)
            .Select(x => new InstallationChecklistItemDto { Id = x.Id, Item = x.Item, IsCompleted = x.IsCompleted })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InstallationPhotoDto>> ListPhotosAsync(
        Guid societyId,
        Guid installationId,
        CancellationToken cancellationToken = default)
    {
        _ = await LoadInstallationAsync(societyId, installationId, cancellationToken);
        var rows = await _app.InstallationPhotos.AsNoTracking()
            .Where(p => p.InstallationId == installationId && p.SocietyId == societyId)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(p => MapPhotoDto(p)).ToList();
    }

    public async Task<InstallationPhotoDto> UploadPhotoAsync(
        Guid societyId,
        Guid installationId,
        string fileName,
        string contentType,
        long length,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        _ = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);

        var stored = await _files.SaveAsync(new FileUploadInput
        {
            SocietyId        = societyId,
            RelativeFolder     = $"installations/{installationId:N}/photos",
            OriginalFileName   = fileName,
            ContentType        = contentType,
            Length             = length,
            Content            = content,
            ImagesOnly         = true
        }, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var photo = new InstallationPhoto
        {
            SocietyId       = societyId,
            InstallationId  = installationId,
            Url             = stored.PublicPath,
            UploadedAt      = now,
            CreatedAt       = now
        };

        _app.InstallationPhotos.Add(photo);
        await _app.SaveChangesAsync(cancellationToken);

        return MapPhotoDto(photo, stored);
    }

    public async Task DeletePhotoAsync(
        Guid societyId,
        Guid installationId,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        _ = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);

        var photo = await _app.InstallationPhotos
            .FirstOrDefaultAsync(p => p.Id == photoId && p.InstallationId == installationId && p.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PHOTO_NOT_FOUND", "Installation photo not found.", 404);

        await _files.DeleteByPublicPathAsync(photo.Url, cancellationToken);
        photo.IsDeleted = true;
        photo.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }

    private InstallationPhotoDto MapPhotoDto(InstallationPhoto p, StoredFileResult? stored = null) => new()
    {
        Id          = p.Id,
        Url         = _files.ToAbsoluteUrl(p.Url),
        FileName    = stored?.StoredFileName ?? Path.GetFileName(p.Url),
        ContentType = stored?.ContentType ?? "application/octet-stream",
        SizeBytes   = stored?.SizeBytes ?? 0,
        UploadedAt  = p.UploadedAt
    };

    private async Task EnsureProjectAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken)
    {
        if (!await _app.Projects.AnyAsync(p => p.Id == projectId && p.SocietyId == societyId, cancellationToken))
            throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);
    }

    private async Task EnsureUserInSocietyAsync(Guid societyId, Guid userId, CancellationToken cancellationToken)
    {
        var ok = await _core.UserSocieties.AnyAsync(
            x => x.UserId == userId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken);

        if (!ok)
            throw new AppException("USER_NOT_IN_SOCIETY", "The selected user is not a member of this society.", 400);
    }

    private async Task<Installation> LoadInstallationAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken) =>
        await _app.Installations.AsNoTracking()
            .Include(x => x.Checklist)
            .Include(x => x.Photos)
            .Include(x => x.Project).ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(x => x.Id == installationId && x.SocietyId == societyId, cancellationToken)
        ?? throw new AppException("INSTALLATION_NOT_FOUND", "Installation not found.", 404);

    private async Task<Installation> LoadInstallationTrackedAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken) =>
        await _app.Installations
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == installationId && x.SocietyId == societyId, cancellationToken)
        ?? throw new AppException("INSTALLATION_NOT_FOUND", "Installation not found.", 404);

    private async Task<InstallationDto> MapDetailAsync(Installation x, CancellationToken cancellationToken)
    {
        var techName = await _core.Users.AsNoTracking()
            .Where(u => u.Id == x.TechnicianId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        return new InstallationDto
        {
            Id = x.Id,
            ProjectId = x.ProjectId,
            ProjectReference = x.Project.Reference,
            ProjectName = x.Project.Name,
            ClientName = x.Project.Client?.Name ?? string.Empty,
            TechnicianId = x.TechnicianId,
            TechnicianName = techName,
            Date = x.Date,
            Status = x.Status,
            Checklist = x.Checklist
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Item)
                .Select(c => new InstallationChecklistItemDto { Id = c.Id, Item = c.Item, IsCompleted = c.IsCompleted })
                .ToList(),
            Photos = x.Photos
                .OrderByDescending(p => p.UploadedAt)
                .Select(p => MapPhotoDto(p))
                .ToList(),
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }

    private static InstallationListItemDto MapListItem(Installation x, string? technicianName)
    {
        var checklist = x.Checklist;
        return new InstallationListItemDto
        {
            Id = x.Id,
            ProjectId = x.ProjectId,
            ProjectReference = x.Project.Reference,
            ProjectName = x.Project.Name,
            ClientName = x.Project.Client?.Name ?? string.Empty,
            TechnicianId = x.TechnicianId,
            TechnicianName = technicianName,
            Date = x.Date,
            Status = x.Status,
            ChecklistCompleted = checklist.Count(c => c.IsCompleted),
            ChecklistTotal = checklist.Count,
            CreatedAt = x.CreatedAt
        };
    }
}
