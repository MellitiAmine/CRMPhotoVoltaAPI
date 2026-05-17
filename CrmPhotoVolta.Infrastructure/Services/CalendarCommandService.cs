using CrmPhotoVolta.Application.Crm.Calendar;
using CrmPhotoVolta.Application.Crm.Leads;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class CalendarCommandService : ICalendarCommandService
{
    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;
    private readonly ILeadJournalService _leadJournal;

    public CalendarCommandService(AppDbContext app, CoreDbContext core, ILeadJournalService leadJournal)
    {
        _app = app;
        _core = core;
        _leadJournal = leadJournal;
    }

    public async Task<CalendarEventDto> CreateAsync(
        Guid societyId,
        Guid creatorId,
        CreateCalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, creatorId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException("TITLE_REQUIRED", "Event title is required.", 400);

        if (request.EndDate <= request.StartDate)
            throw new AppException("INVALID_DATE_RANGE", "End date must be after start date.", 400);

        var validTypes = new[] { "meeting", "reminder", "activity" };
        if (!validTypes.Contains(request.Type.ToLowerInvariant()))
            throw new AppException("INVALID_EVENT_TYPE", "Type must be 'meeting', 'reminder', or 'activity'.", 400);

        // Participants: any active (non-deleted) society member — same pool as commercials / picker.
        if (request.Participants.Count > 0)
        {
            var eligibleUserIds = await GetEligibleSocietyParticipantIdsAsync(societyId, cancellationToken);
            var invalid = request.Participants.Where(uid => !eligibleUserIds.Contains(uid)).ToList();
            if (invalid.Count > 0)
                throw new AppException("INVALID_PARTICIPANT", "One or more participants do not belong to this society.", 400);
        }

        if (request.LeadId is { } leadId)
        {
            var actorIsAdmin = await IsSocietyAdminAsync(societyId, creatorId, cancellationToken);
            var leadOk = actorIsAdmin
                ? await _app.Leads.AsNoTracking()
                    .AnyAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
                : await _app.Leads.AsNoTracking()
                    .AnyAsync(x => x.Id == leadId && x.SocietyId == societyId
                        && ((x.CreatedById.HasValue && relatedUserIds.Contains(x.CreatedById.Value))
                            || (x.AssignedToUserId.HasValue && relatedUserIds.Contains(x.AssignedToUserId.Value))), cancellationToken);
            if (!leadOk)
                throw new AppException("INVALID_LEAD", "Lead not found in this society.", 400);
        }

        var participants = request.Participants.Distinct().ToList();
        var assignee = ResolveAssigneeOrThrow(participants, request.AssignedToUserId);

        var ev = new CalendarEvent
        {
            SocietyId = societyId,
            Title = request.Title.Trim(),
            Type = request.Type.ToLowerInvariant(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Description = request.Description?.Trim(),
            LeadId = request.LeadId,
            Participants = participants,
            AssignedToUserId = assignee,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = creatorId
        };

        _app.Events.Add(ev);

        if (request.LeadId is Guid linkedLeadId)
        {
            _leadJournal.Stage(
                societyId,
                linkedLeadId,
                creatorId,
                LeadJournalActions.CalendarEventCreated,
                "calendar_event",
                ev.Id,
                new { title = ev.Title, type = ev.Type, startDate = ev.StartDate, endDate = ev.EndDate });
        }

        await _app.SaveChangesAsync(cancellationToken);

        return new CalendarEventDto
        {
            Id = ev.Id,
            Title = ev.Title,
            Type = ev.Type,
            StartDate = ev.StartDate,
            EndDate = ev.EndDate,
            Description = ev.Description,
            AssignedToUserId = ev.AssignedToUserId,
            Participants = ev.Participants,
            CreatedById = ev.CreatedById,
            LeadId = ev.LeadId
        };
    }

    public async Task DeleteAsync(
        Guid societyId,
        Guid eventId,
        Guid requestingUserId,
        bool requestingUserIsAdmin,
        CancellationToken cancellationToken = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, requestingUserId, cancellationToken);
        var ev = await _app.Events
            .FirstOrDefaultAsync(x => x.Id == eventId && x.SocietyId == societyId, cancellationToken);

        if (ev is null)
            throw new AppException("EVENT_NOT_FOUND", "Calendar event not found.", 404);

        var canManage = (ev.CreatedById.HasValue && relatedUserIds.Contains(ev.CreatedById.Value))
            || (ev.AssignedToUserId.HasValue && relatedUserIds.Contains(ev.AssignedToUserId.Value))
            || ev.Participants.Any(p => relatedUserIds.Contains(p));
        if (!canManage)
            throw new AppException("FORBIDDEN", "You are not allowed to delete this event.", 403);

        if (ev.LeadId is Guid jl)
        {
            _leadJournal.Stage(
                societyId,
                jl,
                requestingUserId,
                LeadJournalActions.CalendarEventDeleted,
                "calendar_event",
                ev.Id,
                new { title = ev.Title });
        }

        ev.IsDeleted = true;
        ev.UpdatedAt = DateTimeOffset.UtcNow;
        ev.UpdatedById = requestingUserId;

        await _app.SaveChangesAsync(cancellationToken);
    }

    public async Task<CalendarEventDto> UpdateAsync(
        Guid societyId,
        Guid eventId,
        Guid requestingUserId,
        UpdateCalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, requestingUserId, cancellationToken);
        var ev = await _app.Events.FirstOrDefaultAsync(x => x.Id == eventId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("EVENT_NOT_FOUND", "Calendar event not found.", 404);

        var canManage = (ev.CreatedById.HasValue && relatedUserIds.Contains(ev.CreatedById.Value))
            || (ev.AssignedToUserId.HasValue && relatedUserIds.Contains(ev.AssignedToUserId.Value))
            || ev.Participants.Any(p => relatedUserIds.Contains(p));
        if (!canManage)
            throw new AppException("FORBIDDEN", "You are not allowed to update this event.", 403);

        var previousLeadId = ev.LeadId;

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException("TITLE_REQUIRED", "Event title is required.", 400);
        if (request.EndDate <= request.StartDate)
            throw new AppException("INVALID_DATE_RANGE", "End date must be after start date.", 400);

        var validTypes = new[] { "meeting", "reminder", "activity" };
        if (!validTypes.Contains(request.Type.ToLowerInvariant()))
            throw new AppException("INVALID_EVENT_TYPE", "Type must be 'meeting', 'reminder', or 'activity'.", 400);

        if (request.Participants.Count > 0)
        {
            var eligibleUserIds = await GetEligibleSocietyParticipantIdsAsync(societyId, cancellationToken);
            var invalid = request.Participants.Where(uid => !eligibleUserIds.Contains(uid)).ToList();
            if (invalid.Count > 0)
                throw new AppException("INVALID_PARTICIPANT", "One or more participants do not belong to this society.", 400);
        }

        if (request.LeadId is { } leadId)
        {
            var actorIsAdmin = await IsSocietyAdminAsync(societyId, requestingUserId, cancellationToken);
            var leadOk = actorIsAdmin
                ? await _app.Leads.AsNoTracking()
                    .AnyAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
                : await _app.Leads.AsNoTracking()
                    .AnyAsync(x => x.Id == leadId && x.SocietyId == societyId
                        && ((x.CreatedById.HasValue && relatedUserIds.Contains(x.CreatedById.Value))
                            || (x.AssignedToUserId.HasValue && relatedUserIds.Contains(x.AssignedToUserId.Value))), cancellationToken);
            if (!leadOk)
                throw new AppException("INVALID_LEAD", "Lead not found in this scope.", 400);
        }

        ev.Title = request.Title.Trim();
        ev.Type = request.Type.ToLowerInvariant();
        ev.StartDate = request.StartDate;
        ev.EndDate = request.EndDate;
        ev.Description = request.Description?.Trim();
        ev.LeadId = request.LeadId;
        var participants = request.Participants.Distinct().ToList();
        ev.Participants = participants;
        ev.AssignedToUserId = ResolveAssigneeOrThrow(participants, request.AssignedToUserId);
        ev.UpdatedAt = DateTimeOffset.UtcNow;
        ev.UpdatedById = requestingUserId;

        var journalLead = ev.LeadId ?? previousLeadId;
        if (journalLead is Guid jLead)
        {
            _leadJournal.Stage(
                societyId,
                jLead,
                requestingUserId,
                LeadJournalActions.CalendarEventUpdated,
                "calendar_event",
                ev.Id,
                new
                {
                    title = ev.Title,
                    type = ev.Type,
                    startDate = ev.StartDate,
                    endDate = ev.EndDate,
                    leadIdBefore = previousLeadId,
                    leadIdAfter = ev.LeadId
                });
        }

        await _app.SaveChangesAsync(cancellationToken);

        return new CalendarEventDto
        {
            Id = ev.Id,
            Title = ev.Title,
            Type = ev.Type,
            StartDate = ev.StartDate,
            EndDate = ev.EndDate,
            Description = ev.Description,
            AssignedToUserId = ev.AssignedToUserId,
            Participants = ev.Participants,
            CreatedById = ev.CreatedById,
            LeadId = ev.LeadId
        };
    }

    /// <summary>Primary assignee: explicit field when valid, otherwise first participant.</summary>
    private static Guid? ResolveAssigneeOrThrow(IReadOnlyList<Guid> participants, Guid? requestedAssignee)
    {
        if (participants.Count == 0)
        {
            if (requestedAssignee.HasValue)
                throw new AppException("INVALID_ASSIGNEE", "Cannot set assignee when there are no participants.", 400);
            return null;
        }

        if (!requestedAssignee.HasValue)
            return participants[0];

        if (!participants.Contains(requestedAssignee.Value))
            throw new AppException("INVALID_ASSIGNEE", "Assigned user must be included in participants.", 400);

        return requestedAssignee.Value;
    }

    private async Task<HashSet<Guid>> GetEligibleSocietyParticipantIdsAsync(Guid societyId, CancellationToken cancellationToken)
    {
        var list = await (
            from us in _core.UserSocieties.AsNoTracking()
            join u in _core.Users.AsNoTracking() on us.UserId equals u.Id
            where us.SocietyId == societyId && !us.IsDeleted && !u.IsDeleted && u.IsActive
            select us.UserId
        ).ToListAsync(cancellationToken);

        return list.ToHashSet();
    }

    /// <summary>True when the user has Admin role membership in this society (JWT authorization uses the same lookup).</summary>
    private async Task<bool> IsSocietyAdminAsync(Guid societyId, Guid userId, CancellationToken cancellationToken)
    {
        return await _core.UserSocieties
            .AsNoTracking()
            .Include(x => x.Role)
            .AnyAsync(
                x => !x.IsDeleted
                     && x.SocietyId == societyId
                     && x.UserId == userId
                     && x.Role != null
                     && x.Role.Name == "Admin",
                cancellationToken);
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
}
