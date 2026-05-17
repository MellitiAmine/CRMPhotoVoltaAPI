using CrmPhotoVolta.Application.Crm.Leads;
using CrmPhotoVolta.Application.Crm.Notifications;
using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class LeadWonOrchestrationService : ILeadWonOrchestrationService
{
    private readonly AppDbContext _app;
    private readonly INotificationService _notifications;

    public LeadWonOrchestrationService(AppDbContext app, INotificationService notifications)
    {
        _app = app;
        _notifications = notifications;
    }

    public async Task<LeadWonOrchestrationResult> ProcessAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _app.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var lead = await _app.Leads.FirstOrDefaultAsync(
                    x => x.Id == leadId && x.SocietyId == societyId,
                    cancellationToken)
                ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

            var existingProject = await _app.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SocietyId == societyId && x.LeadId == leadId, cancellationToken);

            if (existingProject is not null)
                throw new AppException(
                    "PROJECT_ALREADY_EXISTS",
                    "A project already exists for this lead.",
                    409);

            var (client, clientCreated) = await ResolveOrCreateClientAsync(lead, societyId, cancellationToken);
            var quote = await ResolveQuoteAsync(societyId, leadId, cancellationToken);
            var dealId = await _app.Deals.AsNoTracking()
                .Where(d => d.SocietyId == societyId && d.LeadId == leadId)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => (Guid?)d.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var reference = await NextProjectReferenceAsync(societyId, cancellationToken);
            var project = new Project
            {
                SocietyId = societyId,
                LeadId = leadId,
                ClientId = client.Id,
                DealId = dealId,
                QuoteId = quote?.Id,
                Name = $"Projet PV — {lead.Name}",
                Reference = reference,
                Address = lead.Address ?? client.Address,
                Status = ProjectStatus.New,
                Priority = lead.Priority,
                Notes = $"Créé automatiquement depuis le lead gagné ({leadId}).",
                SystemSizeKw = lead.EstimatedKw is { } kw ? (decimal)kw : null,
                EstimatedProduction = null,
                TotalHt = quote?.TotalHt ?? 0,
                TotalTva = quote?.TotalTva ?? 0,
                TotalTtc = quote?.TotalTtc ?? 0,
                StartDate = DateOnly.FromDateTime(now.UtcDateTime),
                CommercialUserId = lead.AssignedToUserId,
                ManagerUserId = lead.AssignedToUserId,
                ProgressPercent = 5,
                LastActivityAt = now,
                CreatedAt = now
            };

            _app.Projects.Add(project);
            await _app.SaveChangesAsync(cancellationToken);

            if (quote is not null && quote.ProjectId is null)
            {
                quote.ProjectId = project.Id;
                quote.Status = QuoteStatus.Converted;
                quote.UpdatedAt = now;
            }

            foreach (var (title, description, dueDays) in LeadWonProjectDefaults.DefaultTasks)
            {
                _app.Tasks.Add(new CrmTask
                {
                    SocietyId = societyId,
                    ProjectId = project.Id,
                    Title = title,
                    Description = description,
                    AssignedToUserId = lead.AssignedToUserId,
                    Priority = LeadPriority.High,
                    Status = CrmTaskStatus.Open,
                    DueDate = DateOnly.FromDateTime(now.UtcDateTime.AddDays(dueDays)),
                    CreatedAt = now
                });
            }

            _app.ProjectTimelineEvents.Add(new ProjectTimelineEvent
            {
                SocietyId = societyId,
                ProjectId = project.Id,
                Type = ProjectTimelineEventType.ProjectCreated,
                Description = $"Projet créé depuis le lead «{lead.Name}» (réf. {reference}).",
                CreatedByUserId = actorUserId,
                CreatedAt = now
            });

            lead.Status = LeadStatuses.Gagne;
            lead.UpdatedAt = now;

            _app.LeadActivities.Add(new LeadActivity
            {
                SocietyId = societyId,
                LeadId = leadId,
                Type = LeadActivityType.StatusChange,
                Notes = $"Marked as Won — project {project.Id} created.",
                CreatedByUserId = actorUserId,
                CreatedAt = now,
                CreatedById = actorUserId
            });

            await _app.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await NotifyCommercialAsync(societyId, lead, project, cancellationToken);

            return new LeadWonOrchestrationResult
            {
                ClientId = client.Id,
                ProjectId = project.Id,
                QuoteId = quote?.Id,
                ClientCreated = clientCreated,
                ProjectCreated = true
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<(Client Client, bool Created)> ResolveOrCreateClientAsync(
        Lead lead,
        Guid societyId,
        CancellationToken cancellationToken)
    {
        Client? client = null;

        if (!string.IsNullOrWhiteSpace(lead.Email))
        {
            var email = lead.Email.Trim();
            client = await _app.Clients.FirstOrDefaultAsync(
                x => x.SocietyId == societyId && x.Email != null && x.Email.ToLower() == email.ToLower(),
                cancellationToken);
        }

        if (client is null && !string.IsNullOrWhiteSpace(lead.Phone))
        {
            var phone = lead.Phone.Trim();
            client = await _app.Clients.FirstOrDefaultAsync(
                x => x.SocietyId == societyId && x.Phone != null && x.Phone == phone,
                cancellationToken);
        }

        if (client is not null)
            return (client, false);

        client = new Client
        {
            SocietyId = societyId,
            Name = lead.Name,
            Phone = lead.Phone,
            Email = lead.Email,
            Address = lead.Address,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _app.Clients.Add(client);
        await _app.SaveChangesAsync(cancellationToken);
        return (client, true);
    }

    private async Task<Quote?> ResolveQuoteAsync(Guid societyId, Guid leadId, CancellationToken cancellationToken)
    {
        var accepted = await _app.Quotes
            .Where(q => q.SocietyId == societyId && q.LeadId == leadId && q.Status == QuoteStatus.Accepted)
            .OrderByDescending(q => q.AcceptedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (accepted is not null)
            return accepted;

        return await _app.Quotes
            .Where(q => q.SocietyId == societyId && q.LeadId == leadId)
            .OrderByDescending(q => q.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string> NextProjectReferenceAsync(Guid societyId, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"PRJ-{year}-";

        var existing = await _app.Projects
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.SocietyId == societyId && x.Reference != null && x.Reference.StartsWith(prefix))
            .Select(x => x.Reference!)
            .ToListAsync(cancellationToken);

        return SequentialReferenceGenerator.Next(prefix, existing);
    }

    private async Task NotifyCommercialAsync(
        Guid societyId,
        Lead lead,
        Project project,
        CancellationToken cancellationToken)
    {
        if (lead.AssignedToUserId is not { } uid)
            return;

        var body =
            $"Lead «{lead.Name}» gagné — projet {project.Reference} créé. Statut: {project.Status}.";
        await _notifications.NotifyUserAsync(
            societyId,
            uid,
            "LeadWon",
            "Lead gagné — projet créé",
            body,
            cancellationToken);
    }

}
