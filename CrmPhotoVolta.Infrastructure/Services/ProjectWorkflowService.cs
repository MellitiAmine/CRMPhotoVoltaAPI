using CrmPhotoVolta.Application.Crm.Notifications;
using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class ProjectWorkflowService : IProjectWorkflowService
{
    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;
    private readonly INotificationService _notifications;

    // Allowed status transitions map
    private static readonly Dictionary<ProjectStatus, HashSet<ProjectStatus>> AllowedTransitions = new()
    {
        [ProjectStatus.New]           = new() { ProjectStatus.Study, ProjectStatus.Cancelled },
        [ProjectStatus.Study]         = new() { ProjectStatus.TechnicalVisit, ProjectStatus.Cancelled },
        [ProjectStatus.TechnicalVisit]= new() { ProjectStatus.QuoteSent, ProjectStatus.Negotiation, ProjectStatus.Cancelled },
        [ProjectStatus.QuoteSent]     = new() { ProjectStatus.Negotiation, ProjectStatus.Approved, ProjectStatus.Cancelled },
        [ProjectStatus.Negotiation]   = new() { ProjectStatus.Approved, ProjectStatus.Cancelled },
        [ProjectStatus.Approved]      = new() { ProjectStatus.Planning, ProjectStatus.Cancelled },
        [ProjectStatus.Planning]      = new() { ProjectStatus.Installation, ProjectStatus.Cancelled },
        [ProjectStatus.Installation]  = new() { ProjectStatus.WaitingSteg, ProjectStatus.Activated, ProjectStatus.Cancelled },
        [ProjectStatus.WaitingSteg]   = new() { ProjectStatus.Activated, ProjectStatus.Cancelled },
        [ProjectStatus.Activated]     = new() { ProjectStatus.Completed, ProjectStatus.Sav },
        [ProjectStatus.Completed]     = new() { ProjectStatus.Sav },
        [ProjectStatus.Sav]           = new() { ProjectStatus.Completed, ProjectStatus.Cancelled },
        [ProjectStatus.Cancelled]     = new()
    };

    private static readonly Dictionary<ProjectStatus, int> StatusProgress = new()
    {
        [ProjectStatus.New]           = 5,
        [ProjectStatus.Study]         = 10,
        [ProjectStatus.TechnicalVisit]= 20,
        [ProjectStatus.QuoteSent]     = 30,
        [ProjectStatus.Negotiation]   = 40,
        [ProjectStatus.Approved]      = 50,
        [ProjectStatus.Planning]      = 60,
        [ProjectStatus.Installation]  = 75,
        [ProjectStatus.WaitingSteg]   = 80,
        [ProjectStatus.Activated]     = 90,
        [ProjectStatus.Completed]     = 100,
        [ProjectStatus.Sav]           = 95,
        [ProjectStatus.Cancelled]     = 0
    };

    public ProjectWorkflowService(AppDbContext app, CoreDbContext core, INotificationService notifications)
    {
        _app = app;
        _core = core;
        _notifications = notifications;
    }

    public async Task<ProjectDto> ChangeStatusAsync(
        Guid societyId, Guid projectId, Guid actorUserId,
        ChangeProjectStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var project = await _app.Projects
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        var from = project.Status;
        var to = request.Status;

        if (from == to)
            return await MapProjectDtoAsync(project, cancellationToken);

        if (!AllowedTransitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
            throw new AppException(
                "INVALID_TRANSITION",
                $"Transition from {from} to {to} is not allowed.",
                400);

        var now = DateTimeOffset.UtcNow;
        project.Status = to;
        project.ProgressPercent = StatusProgress.GetValueOrDefault(to, project.ProgressPercent);
        project.LastActivityAt = now;
        project.UpdatedAt = now;

        var description = string.IsNullOrWhiteSpace(request.Comment)
            ? $"Statut changé: {from} → {to}"
            : $"Statut changé: {from} → {to}. {request.Comment.Trim()}";

        _app.ProjectTimelineEvents.Add(new ProjectTimelineEvent
        {
            SocietyId = societyId,
            ProjectId = projectId,
            Type = ProjectTimelineEventType.StatusChanged,
            Description = description,
            CreatedByUserId = actorUserId,
            CreatedAt = now
        });

        await _app.SaveChangesAsync(cancellationToken);

        // Notify commercial if assigned
        if (project.CommercialUserId.HasValue)
        {
            await _notifications.NotifyUserAsync(
                societyId,
                project.CommercialUserId.Value,
                "ProjectStatusChanged",
                $"Projet {project.Reference ?? project.Name} — statut mis à jour",
                description,
                cancellationToken);
        }

        return await MapProjectDtoAsync(project, cancellationToken);
    }

    private async Task<ProjectDto> MapProjectDtoAsync(Project p, CancellationToken cancellationToken)
    {
        var userNames = await ProjectUserNameResolver.LoadNamesAsync(
            _core,
            new[] { p.CommercialUserId, p.ManagerUserId, p.TechnicianUserId },
            cancellationToken);

        return new ProjectDto
        {
            Id = p.Id,
            ClientId = p.ClientId,
            ClientName = p.Client?.Name ?? string.Empty,
            LeadId = p.LeadId,
            QuoteId = p.QuoteId,
            DealId = p.DealId,
            Name = p.Name,
            Reference = p.Reference,
            Address = p.Address,
            Status = p.Status,
            Priority = p.Priority,
            Notes = p.Notes,
            TotalHt = p.TotalHt,
            TotalTva = p.TotalTva,
            TotalTtc = p.TotalTtc,
            SystemSizeKw = p.SystemSizeKw,
            EstimatedProduction = p.EstimatedProduction,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            ExpectedInstallationDate = p.ExpectedInstallationDate,
            ManagerUserId = p.ManagerUserId,
            ManagerName = ProjectUserNameResolver.Resolve(userNames, p.ManagerUserId),
            CommercialUserId = p.CommercialUserId,
            CommercialName = ProjectUserNameResolver.Resolve(userNames, p.CommercialUserId),
            TechnicianUserId = p.TechnicianUserId,
            TechnicianName = ProjectUserNameResolver.Resolve(userNames, p.TechnicianUserId),
            ProgressPercent = p.ProgressPercent,
            LastActivityAt = p.LastActivityAt,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}
