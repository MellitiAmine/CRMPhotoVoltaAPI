using CrmPhotoVolta.Application.Crm.Installations;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class InstallationWorkflowService : IInstallationWorkflowService
{
    private readonly AppDbContext _app;

    public InstallationWorkflowService(AppDbContext app)
    {
        _app = app;
    }

    public async Task<InstallationDto> GetAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken = default)
    {
        var inst = await LoadInstallationAsync(societyId, installationId, cancellationToken);
        TenantGuard.EnsureSameTenant(inst, societyId);
        return Map(inst);
    }

    public async Task<InstallationDto> StartAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken = default)
    {
        var inst = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);
        TenantGuard.EnsureSameTenant(inst, societyId);
        inst.Status = InstallationStatus.InProgress;
        inst.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
        return Map(await LoadInstallationAsync(societyId, installationId, cancellationToken));
    }

    public async Task<InstallationDto> CompleteAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken = default)
    {
        var inst = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);
        TenantGuard.EnsureSameTenant(inst, societyId);
        inst.Status = InstallationStatus.Completed;
        inst.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
        return Map(await LoadInstallationAsync(societyId, installationId, cancellationToken));
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
            row.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _app.SaveChangesAsync(cancellationToken);

        return await _app.InstallationChecklistItems.AsNoTracking()
            .Where(x => x.InstallationId == installationId && x.SocietyId == societyId)
            .OrderBy(x => x.Item)
            .Select(x => new InstallationChecklistItemDto { Id = x.Id, Item = x.Item, IsCompleted = x.IsCompleted })
            .ToListAsync(cancellationToken);
    }

    public async Task<InstallationPhotoDto> AddPhotoAsync(
        Guid societyId,
        Guid installationId,
        AddInstallationPhotoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            throw new AppException("VALIDATION_ERROR", "Url is required.", 400);

        _ = await LoadInstallationTrackedAsync(societyId, installationId, cancellationToken);

        var photo = new InstallationPhoto
        {
            SocietyId = societyId,
            InstallationId = installationId,
            Url = request.Url.Trim(),
            UploadedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.InstallationPhotos.Add(photo);
        await _app.SaveChangesAsync(cancellationToken);

        return new InstallationPhotoDto
        {
            Id = photo.Id,
            Url = photo.Url,
            UploadedAt = photo.UploadedAt
        };
    }

    private async Task<Installation> LoadInstallationAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken) =>
        await _app.Installations.AsNoTracking()
            .Include(x => x.Checklist)
            .Include(x => x.Photos)
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == installationId && x.SocietyId == societyId, cancellationToken)
        ?? throw new AppException("INSTALLATION_NOT_FOUND", "Installation not found.", 404);

    private async Task<Installation> LoadInstallationTrackedAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken) =>
        await _app.Installations
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == installationId && x.SocietyId == societyId, cancellationToken)
        ?? throw new AppException("INSTALLATION_NOT_FOUND", "Installation not found.", 404);

    private static InstallationDto Map(Installation x) => new()
    {
        Id = x.Id,
        ProjectId = x.ProjectId,
        TechnicianId = x.TechnicianId,
        Date = x.Date,
        Status = x.Status,
        Checklist = x.Checklist
            .OrderBy(c => c.Item)
            .Select(c => new InstallationChecklistItemDto { Id = c.Id, Item = c.Item, IsCompleted = c.IsCompleted })
            .ToList(),
        Photos = x.Photos
            .OrderByDescending(p => p.UploadedAt)
            .Select(p => new InstallationPhotoDto { Id = p.Id, Url = p.Url, UploadedAt = p.UploadedAt })
            .ToList()
    };
}
