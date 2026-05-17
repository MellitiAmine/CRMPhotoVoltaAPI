using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Leads;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Automation;
using CrmPhotoVolta.Application.Scoring;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class LeadService : ILeadService
{
    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;
    private readonly ILeadScoringService _scoring;
    private readonly ILeadSdAutomationService _sdAutomation;
    private readonly ILeadJournalService _journal;
    private readonly ILeadWonOrchestrationService _wonOrchestration;

    public LeadService(
        AppDbContext app,
        CoreDbContext core,
        ILeadScoringService scoring,
        ILeadSdAutomationService sdAutomation,
        ILeadJournalService journal,
        ILeadWonOrchestrationService wonOrchestration)
    {
        _app = app;
        _core = core;
        _scoring = scoring;
        _sdAutomation = sdAutomation;
        _journal = journal;
        _wonOrchestration = wonOrchestration;
    }

    private static bool IsLegacyAuditActivityType(LeadActivityType t) =>
        t is LeadActivityType.Assignment or LeadActivityType.StatusChange or LeadActivityType.Converted;

    public async Task<(IReadOnlyList<LeadListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        Guid actorUserId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, cancellationToken);
        var query = _app.Leads.AsNoTracking()
            .Where(x => x.SocietyId == societyId)
            .Where(x =>
                (x.CreatedById.HasValue && relatedUserIds.Contains(x.CreatedById.Value))
                || (x.AssignedToUserId.HasValue && relatedUserIds.Contains(x.AssignedToUserId.Value)));

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var s = pagination.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s) ||
                (x.Email != null && x.Email.ToLower().Contains(s)) ||
                (x.Phone != null && x.Phone.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);

        query = pagination.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? ApplySortAsc(query, pagination.SortBy)
            : ApplySortDesc(query, pagination.SortBy);

        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new LeadListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                Address = x.Address,
                Status = x.Status,
                AssignedToUserId = x.AssignedToUserId,
                CreatedAt = x.CreatedAt,
                MonthlyBillEur = x.MonthlyBillEur,
                EstimatedKw = x.EstimatedKw,
                MontantEstimé = x.MontantEstimé,
                Lvi = x.Lvi,
                Sd = x.Sd,
                ScoredAt = x.ScoredAt,
                Temperature = x.Temperature,
                Priority = x.Priority,
                Tags = x.Tags
            })
            .ToListAsync(cancellationToken);

        return (items, pagination.ToMeta(total));
    }

    public async Task<LeadDto> GetAsync(Guid societyId, Guid actorUserId, Guid leadId, CancellationToken cancellationToken = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, cancellationToken);
        var row = await _app.Leads.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId
                && ((x.CreatedById.HasValue && relatedUserIds.Contains(x.CreatedById.Value))
                    || (x.AssignedToUserId.HasValue && relatedUserIds.Contains(x.AssignedToUserId.Value))), cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        if (row.ScoredAt is null)
            return await RecalculateScoreAsync(societyId, actorUserId, leadId, cancellationToken);

        return Map(row);
    }

    public async Task<LeadDto> CreateAsync(Guid societyId, Guid actorUserId, CreateLeadRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        if (request.AssignedToUserId is { } assignee)
            await EnsureUserInSocietyAsync(societyId, assignee, cancellationToken);

        var lead = new Lead
        {
            SocietyId = societyId,
            Name = request.Name.Trim(),
            Phone = request.Phone?.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(),
            Address = request.Address?.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? LeadStatuses.Nouveau : request.Status.Trim(),
            AssignedToUserId = request.AssignedToUserId,
            MonthlyBillEur = request.MonthlyBillEur,
            EstimatedKw = request.EstimatedKw,
            MontantEstimé = request.MontantEstimé,
            AverageRating = request.AverageRating ?? 0.0,
            BonusQuoteRequested = request.BonusQuoteRequested ?? false,
            BonusBudgetConfirmed = request.BonusBudgetConfirmed ?? false,
            BonusDecisionMaker = request.BonusDecisionMaker ?? false,
            BonusFinancingInterest = request.BonusFinancingInterest ?? false,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = actorUserId
        };

        _app.Leads.Add(lead);
        await _app.SaveChangesAsync(cancellationToken);

        await ApplyScoreAsync(lead.Id, societyId, cancellationToken);

        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == lead.Id, cancellationToken));
    }

    public async Task<LeadDto> UpdateAsync(Guid societyId, Guid actorUserId, Guid leadId, UpdateLeadRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        if (request.AssignedToUserId is { } assignee)
            await EnsureUserInSocietyAsync(societyId, assignee, cancellationToken);

        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, cancellationToken);
        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId
                && ((x.CreatedById.HasValue && relatedUserIds.Contains(x.CreatedById.Value))
                    || (x.AssignedToUserId.HasValue && relatedUserIds.Contains(x.AssignedToUserId.Value))), cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        var prevStatus = lead.Status;
        var prevAssign = lead.AssignedToUserId;

        lead.Name = request.Name.Trim();
        lead.Phone = request.Phone?.Trim();
        lead.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        lead.Address = request.Address?.Trim();
        lead.Status = string.IsNullOrWhiteSpace(request.Status) ? LeadStatuses.Nouveau : request.Status.Trim();
        lead.AssignedToUserId = request.AssignedToUserId;
        lead.MonthlyBillEur = request.MonthlyBillEur;
        lead.EstimatedKw = request.EstimatedKw;
        lead.MontantEstimé = request.MontantEstimé;
        if (request.AverageRating is { } ar) lead.AverageRating = ar;
        if (request.BonusQuoteRequested is { } bq) lead.BonusQuoteRequested = bq;
        if (request.BonusBudgetConfirmed is { } bb) lead.BonusBudgetConfirmed = bb;
        if (request.BonusDecisionMaker is { } bd) lead.BonusDecisionMaker = bd;
        if (request.BonusFinancingInterest is { } bf) lead.BonusFinancingInterest = bf;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        if (prevAssign != lead.AssignedToUserId)
        {
            _journal.Stage(
                societyId,
                leadId,
                actorUserId,
                LeadJournalActions.CommercialAssigned,
                "commercial_user",
                lead.AssignedToUserId,
                new { beforeUserId = prevAssign, afterUserId = lead.AssignedToUserId });
        }

        if (!string.Equals(prevStatus, lead.Status, StringComparison.Ordinal))
        {
            _journal.Stage(
                societyId,
                leadId,
                actorUserId,
                LeadJournalActions.LeadStatusChanged,
                "lead",
                leadId,
                new { before = prevStatus, after = lead.Status });
        }

        await _app.SaveChangesAsync(cancellationToken);

        await ApplyScoreAsync(leadId, societyId, cancellationToken);

        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken));
    }

    public async Task DeleteAsync(Guid societyId, Guid actorUserId, Guid leadId, CancellationToken cancellationToken = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, cancellationToken);
        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId
                && ((x.CreatedById.HasValue && relatedUserIds.Contains(x.CreatedById.Value))
                    || (x.AssignedToUserId.HasValue && relatedUserIds.Contains(x.AssignedToUserId.Value))), cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        var hasDeals = await _app.Deals.AnyAsync(x => x.LeadId == leadId && x.SocietyId == societyId, cancellationToken);
        if (hasDeals)
            throw new AppException("LEAD_HAS_DEALS", "Cannot delete a lead that still has deals linked.", 409);

        lead.IsDeleted = true;
        lead.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeadActivityDto>> ListActivitiesAsync(Guid societyId, Guid actorUserId, Guid leadId, CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);

        return await _app.LeadActivities.AsNoTracking()
            .Where(x => x.LeadId == leadId && x.SocietyId == societyId)
            .Where(x =>
                x.Type != LeadActivityType.Assignment
                && x.Type != LeadActivityType.StatusChange
                && x.Type != LeadActivityType.Converted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LeadActivityDto
            {
                Id = x.Id,
                LeadId = x.LeadId,
                Type = x.Type,
                Notes = x.Notes,
                Rating = x.Rating,
                CreatedByUserId = x.CreatedByUserId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<LeadActivityDto> AddActivityAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        AddLeadActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        if ((int)request.Type == 0)
            throw new AppException("VALIDATION_ERROR", "Activity type is required.", 400);

        if (request.Rating is { } rr && (rr < 1 || rr > 5))
            throw new AppException("VALIDATION_ERROR", "Rating must be between 1 and 5.", 400);

        if (IsLegacyAuditActivityType(request.Type) || request.Type == LeadActivityType.MeetingScheduled)
        {
            throw new AppException(
                "INVALID_ACTIVITY_TYPE",
                "This activity type is reserved for the audit journal or the calendar. Log calls/notes from the activity form, and schedule meetings from the calendar.",
                400);
        }

        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);

        var activity = new LeadActivity
        {
            SocietyId = societyId,
            LeadId = leadId,
            Type = request.Type,
            Notes = request.Notes?.Trim(),
            Rating = request.Rating,
            CreatedByUserId = actorUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = actorUserId
        };

        _app.LeadActivities.Add(activity);

        await _app.SaveChangesAsync(cancellationToken);

        await ApplyScoreAsync(leadId, societyId, cancellationToken);

        var row = await _app.LeadActivities.AsNoTracking().FirstAsync(x => x.Id == activity.Id, cancellationToken);
        return MapActivity(row);
    }

    public async Task<LeadActivityDto> UpdateActivityAsync(
        Guid societyId,
        Guid leadId,
        Guid activityId,
        Guid actorUserId,
        UpdateLeadActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        if ((int)request.Type == 0)
            throw new AppException("VALIDATION_ERROR", "Activity type is required.", 400);

        if (request.Rating is { } rr && (rr < 1 || rr > 5))
            throw new AppException("VALIDATION_ERROR", "Rating must be between 1 and 5.", 400);

        if (IsLegacyAuditActivityType(request.Type) || request.Type == LeadActivityType.MeetingScheduled)
        {
            throw new AppException(
                "INVALID_ACTIVITY_TYPE",
                "This activity type is reserved for the audit journal or the calendar.",
                400);
        }

        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);

        var activity = await _app.LeadActivities
            .FirstOrDefaultAsync(x => x.Id == activityId && x.LeadId == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("ACTIVITY_NOT_FOUND", "Activity not found.", 404);

        var beforeType = activity.Type;
        var beforeNotes = activity.Notes;
        var beforeRating = activity.Rating;

        activity.Type = request.Type;
        activity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        activity.Rating = request.Rating is null or 0 ? null : request.Rating;
        activity.UpdatedAt = DateTimeOffset.UtcNow;
        activity.UpdatedById = actorUserId;

        _journal.Stage(
            societyId,
            leadId,
            actorUserId,
            LeadJournalActions.ActivityUpdated,
            "lead_activity",
            activity.Id,
            new
            {
                before = new { type = beforeType.ToString(), notes = beforeNotes, rating = beforeRating },
                after = new { type = activity.Type.ToString(), notes = activity.Notes, rating = activity.Rating }
            });

        await _app.SaveChangesAsync(cancellationToken);
        await ApplyScoreAsync(leadId, societyId, cancellationToken);

        var row = await _app.LeadActivities.AsNoTracking().FirstAsync(x => x.Id == activity.Id, cancellationToken);
        return MapActivity(row);
    }

    public async Task DeleteActivityAsync(
        Guid societyId,
        Guid leadId,
        Guid activityId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);

        var activity = await _app.LeadActivities
            .FirstOrDefaultAsync(x => x.Id == activityId && x.LeadId == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("ACTIVITY_NOT_FOUND", "Activity not found.", 404);

        _journal.Stage(
            societyId,
            leadId,
            actorUserId,
            LeadJournalActions.ActivityDeleted,
            "lead_activity",
            activity.Id,
            new { type = activity.Type.ToString(), notes = activity.Notes, rating = activity.Rating });

        activity.IsDeleted = true;
        activity.UpdatedAt = DateTimeOffset.UtcNow;
        activity.UpdatedById = actorUserId;

        await _app.SaveChangesAsync(cancellationToken);
        await ApplyScoreAsync(leadId, societyId, cancellationToken);
    }

    public async Task<LeadDto> AssignAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        AssignLeadRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureUserInSocietyAsync(societyId, request.UserId, cancellationToken);
        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);

        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        var prevAssign = lead.AssignedToUserId;

        lead.AssignedToUserId = request.UserId;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        _journal.Stage(
            societyId,
            leadId,
            actorUserId,
            LeadJournalActions.CommercialAssigned,
            "commercial_user",
            request.UserId,
            new { beforeUserId = prevAssign, afterUserId = request.UserId });

        await _app.SaveChangesAsync(cancellationToken);
        await ApplyScoreAsync(leadId, societyId, cancellationToken);
        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken));
    }

    public async Task<ConvertLeadResultDto> ConvertAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        ConvertLeadRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);
        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        if (lead.Status == LeadStatuses.Converted)
            throw new AppException("LEAD_ALREADY_CONVERTED", "Lead is already converted.", 409);

        var client = new Client
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

        Guid? dealId = null;
        if (request.CreateDeal)
        {
            var title = string.IsNullOrWhiteSpace(request.DealTitle) ? $"Deal — {lead.Name}" : request.DealTitle!.Trim();
            var deal = new Deal
            {
                SocietyId = societyId,
                LeadId = leadId,
                Title = title,
                Stage = DealStages.New,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _app.Deals.Add(deal);
            await _app.SaveChangesAsync(cancellationToken);
            dealId = deal.Id;
        }

        lead.Status = LeadStatuses.Converted;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        _journal.Stage(
            societyId,
            leadId,
            actorUserId,
            LeadJournalActions.LeadConverted,
            "client",
            client.Id,
            new { dealId = dealId, clientId = client.Id });

        await _app.SaveChangesAsync(cancellationToken);

        await ApplyScoreAsync(leadId, societyId, cancellationToken);

        return new ConvertLeadResultDto
        {
            Lead = Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken)),
            ClientId = client.Id,
            DealId = dealId
        };
    }

    public async Task<LeadWonResultDto> MarkWonAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);

        var orchestration = await _wonOrchestration.ProcessAsync(societyId, leadId, actorUserId, cancellationToken);

        _journal.Stage(
            societyId,
            leadId,
            actorUserId,
            LeadJournalActions.LeadMarkedWon,
            "lead",
            leadId,
            new
            {
                status = LeadStatuses.Gagne,
                projectId = orchestration.ProjectId,
                clientId = orchestration.ClientId
            });

        await _app.SaveChangesAsync(cancellationToken);
        await ApplyScoreAsync(leadId, societyId, cancellationToken);

        return new LeadWonResultDto
        {
            Lead = await GetAsync(societyId, actorUserId, leadId, cancellationToken),
            ClientId = orchestration.ClientId,
            ProjectId = orchestration.ProjectId,
            QuoteId = orchestration.QuoteId,
            ClientCreated = orchestration.ClientCreated,
            ProjectCreated = orchestration.ProjectCreated
        };
    }

    public async Task<LeadDto> MarkLostAsync(Guid societyId, Guid leadId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);
        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        lead.Status = LeadStatuses.Perdu;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        _journal.Stage(
            societyId,
            leadId,
            actorUserId,
            LeadJournalActions.LeadMarkedLost,
            "lead",
            leadId,
            new { status = lead.Status });

        await _app.SaveChangesAsync(cancellationToken);
        await ApplyScoreAsync(leadId, societyId, cancellationToken);
        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken));
    }

    public async Task<LeadActivityDto> AddNoteAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        AddLeadNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            throw new AppException("VALIDATION_ERROR", "Note body is required.", 400);

        return await AddActivityAsync(societyId, leadId, actorUserId,
            new AddLeadActivityRequest { Type = LeadActivityType.Note, Notes = request.Body.Trim() },
            cancellationToken);
    }

    public async Task<IReadOnlyList<LeadTimelineEntryDto>> GetTimelineAsync(
        Guid societyId,
        Guid actorUserId,
        Guid leadId,
        CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);

        var activityRows = await _app.LeadActivities.AsNoTracking()
            .Where(x => x.LeadId == leadId && x.SocietyId == societyId)
            .Where(x =>
                x.Type != LeadActivityType.Assignment
                && x.Type != LeadActivityType.StatusChange
                && x.Type != LeadActivityType.Converted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var actorIds = activityRows.Select(x => x.CreatedByUserId).Where(x => x != Guid.Empty).Distinct().ToList();
        var labels = await ResolveSocietyUserLabelsAsync(societyId, actorIds, cancellationToken);

        var activities = activityRows.ConvertAll(x =>
        {
            labels.TryGetValue(x.CreatedByUserId, out var lab);
            return new LeadTimelineEntryDto
            {
                Kind = "activity",
                At = x.CreatedAt,
                Title = x.Type.ToString(),
                Detail = x.Notes,
                RefId = x.Id,
                CreatedByUserId = x.CreatedByUserId,
                CreatorDisplayName = lab.FullName,
                CreatorRoleLabel = lab.RoleLabel,
                Rating = x.Rating,
                ActivityType = x.Type
            };
        });

        var quotes = await _app.Quotes.AsNoTracking()
            .Where(x => x.SocietyId == societyId && x.LeadId == leadId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LeadTimelineEntryDto
            {
                Kind = "quote",
                At = x.CreatedAt,
                Title = $"Quote {x.QuoteNumber}",
                Detail = $"{x.Status} — {x.TotalAmount} {x.Currency}",
                RefId = x.Id,
                CreatedByUserId = null,
                CreatorDisplayName = null,
                CreatorRoleLabel = null,
                Rating = null,
                ActivityType = null
            })
            .ToListAsync(cancellationToken);

        return activities.Concat(quotes).OrderByDescending(x => x.At).ToList();
    }

    public async Task<IReadOnlyList<LeadJournalEntryDto>> GetJournalAsync(
        Guid societyId,
        Guid actorUserId,
        Guid leadId,
        CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);
        var raw = await _journal.ListForLeadAsync(societyId, leadId, cancellationToken);

        var idSet = new HashSet<Guid>();
        foreach (var e in raw)
        {
            if (e.ActorUserId != Guid.Empty)
                idSet.Add(e.ActorUserId);
            LeadJournalFrenchSummary.CollectUserIds(e.Action, e.MetadataJson, idSet);
        }

        var labels = await ResolveSocietyUserLabelsAsync(societyId, idSet.ToList(), cancellationToken);
        await MergeUserNamesWithoutSocietyRoleAsync(idSet, labels, cancellationToken);

        var list = new List<LeadJournalEntryDto>(raw.Count);
        foreach (var e in raw)
        {
            labels.TryGetValue(e.ActorUserId, out var actorTuple);
            var summary = LeadJournalFrenchSummary.Build(e.Action, e.MetadataJson, labels);
            if (string.IsNullOrWhiteSpace(summary))
                summary = LeadJournalFrenchSummary.FallbackSummary(e.Action);
            if (e.Action == LeadJournalActions.ActivityCreated)
                summary = "Création d’activité (entrée historique).";

            list.Add(new LeadJournalEntryDto
            {
                Id = e.Id,
                LeadId = e.LeadId,
                Action = e.Action,
                ActorUserId = e.ActorUserId,
                CreatedAt = e.CreatedAt,
                RelatedEntityType = e.RelatedEntityType,
                RelatedEntityId = e.RelatedEntityId,
                MetadataJson = e.MetadataJson,
                ActorDisplayName = e.ActorUserId == Guid.Empty ? null : actorTuple.FullName,
                ActorRoleLabel = e.ActorUserId == Guid.Empty ? null : actorTuple.RoleLabel,
                SummaryFr = summary
            });
        }

        return list;
    }

    /// <summary>Fills display names for users not linked via <see cref="UserSociety"/> (role: Utilisateur).</summary>
    private async Task MergeUserNamesWithoutSocietyRoleAsync(
        IReadOnlyCollection<Guid> candidateIds,
        Dictionary<Guid, (string FullName, string RoleLabel)> labels,
        CancellationToken cancellationToken)
    {
        var missing = candidateIds
            .Where(id => id != Guid.Empty && !labels.ContainsKey(id))
            .Distinct()
            .ToList();
        if (missing.Count == 0)
            return;

        var rows = await _core.Users.AsNoTracking()
            .Where(u => !u.IsDeleted && missing.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(cancellationToken);

        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.FullName))
                continue;
            labels[r.Id] = (r.FullName.Trim(), "Utilisateur");
        }
    }

    public async Task<LeadDto> RecalculateScoreAsync(Guid societyId, Guid actorUserId, Guid leadId, CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);
        await ApplyScoreAsync(leadId, societyId, cancellationToken);
        var row = await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken);
        return Map(row);
    }

    private static LeadActivityDto MapActivity(LeadActivity row) => new()
    {
        Id = row.Id,
        LeadId = row.LeadId,
        Type = row.Type,
        Notes = row.Notes,
        Rating = row.Rating,
        CreatedByUserId = row.CreatedByUserId,
        CreatedAt = row.CreatedAt
    };

    private async Task<Dictionary<Guid, (string FullName, string RoleLabel)>> ResolveSocietyUserLabelsAsync(
        Guid societyId,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var distinct = userIds.Where(x => x != Guid.Empty).Distinct().ToList();
        if (distinct.Count == 0)
            return new Dictionary<Guid, (string FullName, string RoleLabel)>();

        var rows = await (
            from us in _core.UserSocieties.AsNoTracking()
            where us.SocietyId == societyId && !us.IsDeleted && distinct.Contains(us.UserId)
            join u in _core.Users.AsNoTracking() on us.UserId equals u.Id
            where !u.IsDeleted
            join r in _core.Roles.AsNoTracking() on us.RoleId equals r.Id
            select new { us.UserId, u.FullName, r.RoleType, r.Name }
        ).ToListAsync(cancellationToken);

        var dict = new Dictionary<Guid, (string FullName, string RoleLabel)>();
        foreach (var x in rows)
        {
            if (dict.ContainsKey(x.UserId))
                continue;
            dict[x.UserId] = (x.FullName, RoleTypeToLabel(x.RoleType, x.Name));
        }

        return dict;
    }

    private static string RoleTypeToLabel(RoleType t, string roleName) =>
        t switch
        {
            RoleType.Admin => "Administrateur",
            RoleType.Manager => "Manager",
            RoleType.Commercial => "Commercial",
            _ => string.IsNullOrWhiteSpace(roleName) ? "Membre" : roleName
        };

    private async Task ApplyScoreAsync(Guid leadId, Guid societyId, CancellationToken cancellationToken)
    {
        var lead = await _app.Leads.FirstAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken);

        var activities = await _app.LeadActivities.AsNoTracking()
            .Where(x => x.LeadId == leadId && x.SocietyId == societyId)
            .Where(x =>
                x.Type != LeadActivityType.Assignment
                && x.Type != LeadActivityType.StatusChange
                && x.Type != LeadActivityType.Converted)
            .ToListAsync(cancellationToken);

        var snapshot = _scoring.Calculate(lead, activities);
        lead.ApplyScoring(in snapshot);

        await _app.SaveChangesAsync(cancellationToken);

        await _sdAutomation.ProcessAfterScoringAsync(lead, snapshot, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureLeadAsync(Guid societyId, Guid actorUserId, Guid leadId, CancellationToken cancellationToken)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, cancellationToken);
        if (!await _app.Leads.AnyAsync(
            x => x.Id == leadId && x.SocietyId == societyId
                && ((x.CreatedById.HasValue && relatedUserIds.Contains(x.CreatedById.Value))
                    || (x.AssignedToUserId.HasValue && relatedUserIds.Contains(x.AssignedToUserId.Value))),
            cancellationToken))
            throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);
    }

    private async Task<HashSet<Guid>> GetRelatedUserIdsAsync(Guid societyId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var members = await _core.UserSocieties
            .Where(x => x.SocietyId == societyId && !x.IsDeleted)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var memberSet = members.ToHashSet();

        var related = new HashSet<Guid> { actorUserId };
        if (!memberSet.Contains(actorUserId))
            return related;

        var actorCreatorId = await _core.Users
            .Where(u => u.Id == actorUserId && !u.IsDeleted)
            .Select(u => u.CreatedById)
            .FirstOrDefaultAsync(cancellationToken);

        if (actorCreatorId.HasValue && memberSet.Contains(actorCreatorId.Value))
            related.Add(actorCreatorId.Value);

        var creatorCandidates = new List<Guid> { actorUserId };
        if (actorCreatorId.HasValue)
            creatorCandidates.Add(actorCreatorId.Value);

        var subordinateIds = await _core.Users
            .Where(u => !u.IsDeleted && u.CreatedById.HasValue && creatorCandidates.Contains(u.CreatedById.Value))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var uid in subordinateIds)
        {
            if (memberSet.Contains(uid))
                related.Add(uid);
        }

        return related;
    }

    private async Task EnsureUserInSocietyAsync(Guid societyId, Guid userId, CancellationToken cancellationToken)
    {
        var ok = await _core.UserSocieties.AnyAsync(
            x => x.UserId == userId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken);

        if (!ok)
            throw new AppException("ASSIGNEE_NOT_IN_SOCIETY", "The selected user is not a member of this society.", 400);
    }

    private static LeadDto Map(Lead x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Phone = x.Phone,
        Email = x.Email,
        Address = x.Address,
        Status = x.Status,
        AssignedToUserId = x.AssignedToUserId,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt,
        MonthlyBillEur = x.MonthlyBillEur,
        EstimatedKw = x.EstimatedKw,
        MontantEstimé = x.MontantEstimé,
        AverageRating = x.AverageRating,
        BonusQuoteRequested = x.BonusQuoteRequested,
        BonusBudgetConfirmed = x.BonusBudgetConfirmed,
        BonusDecisionMaker = x.BonusDecisionMaker,
        BonusFinancingInterest = x.BonusFinancingInterest,
        Lvi = x.Lvi,
        Sd = x.Sd,
        ScoredAt = x.ScoredAt,
        Temperature = x.Temperature,
        Priority = x.Priority,
        Tags = x.Tags,
        ScoreBreakdown = x.ScoreBreakdownInteraction is null
            ? null
            : new LeadScoreBreakdownDto
            {
                Interaction = x.ScoreBreakdownInteraction ?? 0,
                Intention = x.ScoreBreakdownIntention ?? 0,
                Satisfaction = x.ScoreBreakdownSatisfaction ?? 0,
                Activity = x.ScoreBreakdownActivity ?? 0,
                Potential = x.ScoreBreakdownPotential ?? 0,
                Penalties = x.ScoreBreakdownPenalties ?? 0
            },
        Recommendations = BuildRecommendations(x)
    };

    private static List<LeadRecommendationDto> BuildRecommendations(Lead lead)
    {
        var list = new List<LeadRecommendationDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sd = lead.Sd ?? 0.0;
        var status = lead.Status?.Trim() ?? string.Empty;
        var penalties = lead.ScoreBreakdownPenalties ?? 0.0;

        void Add(LeadRecommendationDto dto)
        {
            if (seen.Add(dto.Code)) list.Add(dto);
        }

        // 1) Primary next action from SD score band.
        if (sd >= 85.0)
        {
            Add(new LeadRecommendationDto
            {
                Code = "urgent-call",
                Title = "Appel urgent",
                Description = "Lead très chaud: appeler immédiatement pour sécuriser l'engagement.",
                ActionLabel = "Appeler maintenant",
                Priority = "Critical"
            });
        }
        else if (sd >= 70.0)
        {
            Add(new LeadRecommendationDto
            {
                Code = "call-soon",
                Title = "Appeler bientôt",
                Description = "Intention élevée: confirmer budget, décideur et prochain rendez-vous.",
                ActionLabel = "Planifier un appel",
                Priority = "High"
            });
        }
        else if (sd >= 50.0)
        {
            Add(new LeadRecommendationDto
            {
                Code = "whatsapp-follow-up",
                Title = "Message WhatsApp",
                Description = "Relance courte et directe pour obtenir une réponse rapide.",
                ActionLabel = "Envoyer un WhatsApp",
                Priority = "Medium"
            });
        }
        else
        {
            Add(new LeadRecommendationDto
            {
                Code = "nurturing-sequence",
                Title = "Nurturing / Marketing",
                Description = "Lead froid: basculer vers une séquence de contenu + relance planifiée.",
                ActionLabel = "Lancer la séquence",
                Priority = "Low"
            });
        }

        // 2) Funnel stage recommendations (can stack with primary action).
        if (status is LeadStatuses.Qualifie or LeadStatuses.Proposition or LeadStatuses.Negociation || lead.BonusQuoteRequested)
        {
            Add(new LeadRecommendationDto
            {
                Code = "send-quote",
                Title = "Envoyer devis",
                Description = "Signaux de maturité commerciale: formaliser une proposition rapidement.",
                ActionLabel = "Préparer le devis",
                Priority = "High"
            });
        }

        if (status is LeadStatuses.Qualifie or LeadStatuses.Proposition)
        {
            Add(new LeadRecommendationDto
            {
                Code = "schedule-technical-visit",
                Title = "Planifier visite technique",
                Description = "Valider la faisabilité terrain pour accélérer la conversion.",
                ActionLabel = "Planifier la visite",
                Priority = "Medium"
            });
        }

        if (status == LeadStatuses.Negociation)
        {
            Add(new LeadRecommendationDto
            {
                Code = "negotiation-push",
                Title = "Relance négociation",
                Description = "Traiter les objections prix/délais et proposer une option alternative claire.",
                ActionLabel = "Relancer la négociation",
                Priority = "High"
            });
        }

        // 3) Bonus signal driven recommendations.
        if (lead.BonusBudgetConfirmed)
        {
            Add(new LeadRecommendationDto
            {
                Code = "budget-validated",
                Title = "Finaliser proposition budget",
                Description = "Budget confirmé: proposer une configuration finale et un planning d'exécution.",
                ActionLabel = "Finaliser la proposition",
                Priority = "High"
            });
        }

        if (lead.BonusFinancingInterest)
        {
            Add(new LeadRecommendationDto
            {
                Code = "financing-plan",
                Title = "Proposer plan de financement",
                Description = "Présenter une simulation de mensualités pour réduire les frictions de décision.",
                ActionLabel = "Envoyer simulation",
                Priority = "Medium"
            });
        }

        // 4) Hygiene / operational recommendations.
        if (lead.AssignedToUserId is null)
        {
            Add(new LeadRecommendationDto
            {
                Code = "assign-owner",
                Title = "Assigner un commercial",
                Description = "Aucun responsable assigné: affecter un owner pour garantir le suivi.",
                ActionLabel = "Assigner maintenant",
                Priority = "High"
            });
        }

        if (penalties > 0.0)
        {
            Add(new LeadRecommendationDto
            {
                Code = "reactivation-plan",
                Title = "Plan de réactivation",
                Description = "Des pénalités sont appliquées: ajouter une action rapide pour relancer le lead.",
                ActionLabel = "Relancer aujourd'hui",
                Priority = "Medium"
            });
        }

        Add(new LeadRecommendationDto
        {
            Code = "add-note",
            Title = "Ajouter note commerciale",
            Description = "Tracer le contexte, les objections et la prochaine action pour fiabiliser le suivi.",
            ActionLabel = "Ajouter une note",
            Priority = "Low"
        });

        var recalcAnchor = lead.ScoredAt ?? lead.UpdatedAt ?? lead.CreatedAt;
        if (DateTimeOffset.UtcNow - recalcAnchor > TimeSpan.FromDays(3))
        {
            Add(new LeadRecommendationDto
            {
                Code = "recalculate-score",
                Title = "Recalculer score",
                Description = "Le score n'a pas été rafraîchi récemment.",
                ActionLabel = "Recalculer",
                Priority = "Low"
            });
        }

        return list;
    }

    // ── Minimum score thresholds per status ───────────────────────────────────
    // When the user manually selects a status, LVI and SD are bumped up to at
    // least the minimum value that corresponds to that stage in the funnel.
    private static readonly Dictionary<string, double> StatusMinScores = new(StringComparer.OrdinalIgnoreCase)
    {
        [LeadStatuses.Nouveau]      = 0.0,
        [LeadStatuses.Contacte]     = 10.0,
        [LeadStatuses.Qualifie]     = 25.0,
        [LeadStatuses.Proposition]  = 40.0,
        [LeadStatuses.Negociation]  = 60.0,
        [LeadStatuses.Installation] = 70.0,
        [LeadStatuses.Gagne]        = 80.0,
        [LeadStatuses.Perdu]        = 0.0,
        [LeadStatuses.Archive]      = 0.0,
    };

    public async Task<LeadDto> ChangeStatusAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        ChangeLeadStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            throw new AppException("VALIDATION_ERROR", "Status is required.", 400);

        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);
        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        var newStatus = request.Status.Trim();
        var oldStatus = lead.Status;

        lead.Status = newStatus;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        // Apply minimum score floor for the selected status
        var minScore = StatusMinScores.TryGetValue(newStatus, out var ms) ? ms : 0.0;
        var newLvi = Math.Max(lead.Lvi ?? 0.0, minScore);
        var newSd  = Math.Max(lead.Sd  ?? 0.0, minScore);

        // Derive temperature/priority from the new (possibly bumped) SD.
        // Temperature and Priority have private setters — use ApplyScoring.
        var effectiveSd = newSd;
        var newTemp = effectiveSd >= 85 ? LeadTemperature.Hot
                    : effectiveSd >= 70 ? LeadTemperature.High
                    : effectiveSd >= 50 ? LeadTemperature.Medium
                    : effectiveSd >= 20 ? LeadTemperature.Low
                    : LeadTemperature.Cold;
        var newPriority = effectiveSd >= 85 ? LeadPriority.Urgent
                        : effectiveSd >= 60 ? LeadPriority.High
                        : LeadPriority.Low;

        var snapshot = new LeadScoreSnapshot
        {
            Lvi = newLvi,
            Sd  = newSd,
            Temperature = newTemp,
            Priority    = newPriority,
            Breakdown   = new LeadScoreBreakdown
            {
                Interaction  = lead.ScoreBreakdownInteraction  ?? 0,
                Intention    = lead.ScoreBreakdownIntention    ?? 0,
                Satisfaction = lead.ScoreBreakdownSatisfaction ?? 0,
                Activity     = lead.ScoreBreakdownActivity     ?? 0,
                Potential    = lead.ScoreBreakdownPotential    ?? 0,
                Penalties    = lead.ScoreBreakdownPenalties    ?? 0,
            }
        };
        lead.ApplyScoring(in snapshot);

        _journal.Stage(
            societyId,
            leadId,
            actorUserId,
            LeadJournalActions.LeadStatusChanged,
            "lead",
            leadId,
            new { before = oldStatus, after = newStatus, minScore });

        await _app.SaveChangesAsync(cancellationToken);

        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken));
    }

    // ── Minimum score floors per temperature level ────────────────────────────
    // When the user manually picks a temperature, LVI and SD are guaranteed
    // to be at least this value.
    private static readonly Dictionary<LeadTemperature, double> TempMinScores = new()
    {
        [LeadTemperature.Cold]   = 0.0,
        [LeadTemperature.Low]    = 20.0,
        [LeadTemperature.Medium] = 50.0,
        [LeadTemperature.High]   = 70.0,
        [LeadTemperature.Hot]    = 85.0,
    };

    public async Task<LeadDto> ChangeTemperatureAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        ChangeLeadTemperatureRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);
        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        var newTemp = request.Temperature;
        var minScore = TempMinScores.TryGetValue(newTemp, out var ms) ? ms : 0.0;

        var newLvi = Math.Max(lead.Lvi ?? 0.0, minScore);
        var newSd  = Math.Max(lead.Sd  ?? 0.0, minScore);

        // Priority is derived from the new SD value
        var newPriority = newSd >= 85 ? LeadPriority.Urgent
                        : newSd >= 60 ? LeadPriority.High
                        : LeadPriority.Low;

        // ApplyScoring is the only way to mutate Temperature and Priority
        // (both have private setters on the domain entity).
        var snapshot = new LeadScoreSnapshot
        {
            Lvi         = newLvi,
            Sd          = newSd,
            Temperature = newTemp,
            Priority    = newPriority,
            Breakdown   = new LeadScoreBreakdown
            {
                Interaction  = lead.ScoreBreakdownInteraction  ?? 0,
                Intention    = lead.ScoreBreakdownIntention    ?? 0,
                Satisfaction = lead.ScoreBreakdownSatisfaction ?? 0,
                Activity     = lead.ScoreBreakdownActivity     ?? 0,
                Potential    = lead.ScoreBreakdownPotential    ?? 0,
                Penalties    = lead.ScoreBreakdownPenalties    ?? 0,
            }
        };
        lead.ApplyScoring(in snapshot);

        // Audit trail
        lead.UpdatedAt  = DateTimeOffset.UtcNow;
        lead.UpdatedById = actorUserId;

        _journal.Stage(
            societyId,
            leadId,
            actorUserId,
            LeadJournalActions.LeadTemperatureChanged,
            "lead",
            leadId,
            new
            {
                temperature = newTemp.ToString(),
                minScore,
                lvi = newLvi,
                sd = newSd
            });

        await _app.SaveChangesAsync(cancellationToken);

        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken));
    }

    public async Task<LeadDto> AddTagAsync(
        Guid societyId,
        Guid actorUserId,
        Guid leadId,
        AddLeadTagRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);
        var tag = request.Tag?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(tag))
            throw new AppException("VALIDATION_ERROR", "Tag cannot be empty.", 400);

        if (tag.Length > 50)
            throw new AppException("VALIDATION_ERROR", "Tag must be 50 characters or fewer.", 400);

        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        if (!lead.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            lead.Tags = new List<string>(lead.Tags) { tag };
            lead.UpdatedAt = DateTimeOffset.UtcNow;
            await _app.SaveChangesAsync(cancellationToken);
        }

        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken));
    }

    public async Task<LeadDto> RemoveTagAsync(
        Guid societyId,
        Guid actorUserId,
        Guid leadId,
        string tag,
        CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, actorUserId, leadId, cancellationToken);
        var tagNorm = tag?.Trim().ToLowerInvariant() ?? string.Empty;

        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        lead.Tags = lead.Tags.Where(t => !t.Equals(tagNorm, StringComparison.OrdinalIgnoreCase)).ToList();
        lead.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);

        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken));
    }

    private static IQueryable<Lead> ApplySortAsc(IQueryable<Lead> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderBy(x => x.Name),
            "status" => query.OrderBy(x => x.Status),
            "email" => query.OrderBy(x => x.Email),
            "lvi" => query.OrderBy(x => x.Lvi ?? double.MaxValue),
            _ => query.OrderBy(x => x.CreatedAt)
        };

    private static IQueryable<Lead> ApplySortDesc(IQueryable<Lead> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderByDescending(x => x.Name),
            "status" => query.OrderByDescending(x => x.Status),
            "email" => query.OrderByDescending(x => x.Email),
            "lvi" => query.OrderByDescending(x => x.Lvi ?? -1.0),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };
}
