using CrmPhotoVolta.Application.Crm.Calendar;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class CalendarQueryService : ICalendarQueryService
{
    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;

    public CalendarQueryService(AppDbContext app, CoreDbContext core)
    {
        _app = app;
        _core = core;
    }

    public async Task<IReadOnlyList<CalendarParticipantCandidateDto>> ListParticipantCandidatesAsync(
        Guid societyId,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
                from us in _core.UserSocieties.AsNoTracking()
                join u in _core.Users.AsNoTracking() on us.UserId equals u.Id
                join r in _core.Roles.AsNoTracking() on us.RoleId equals r.Id
                where us.SocietyId == societyId && !us.IsDeleted && !u.IsDeleted && u.IsActive && !r.IsDeleted
                orderby u.FullName
                select new CalendarParticipantCandidateDto(
                    us.UserId,
                    u.FullName ?? string.Empty,
                    u.Email,
                    r.Name,
                    (int)r.RoleType))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<IReadOnlyList<CalendarEventDto>> ListAsync(
        Guid societyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        Guid? technicianId,
        Guid? projectId,
        Guid? leadId,
        Guid? callerUserId,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        var query = _app.Events.AsNoTracking().Where(x => x.SocietyId == societyId);
        var relatedUserIds = callerUserId.HasValue
            ? await GetRelatedUserIdsAsync(societyId, callerUserId.Value, cancellationToken)
            : new HashSet<Guid>();

        if (leadId is Guid lid)
        {
            if (!await CanActorViewLeadAsync(societyId, lid, callerUserId, callerIsAdmin, cancellationToken))
                return Array.Empty<CalendarEventDto>();
            query = query.Where(x => x.LeadId == lid);
        }

        if (from is { } f)
            query = query.Where(x => x.StartDate >= f);
        if (to is { } t)
            query = query.Where(x => x.StartDate <= t);

        if (projectId is { } pid)
        {
            var project = await _app.Projects.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == pid && x.SocietyId == societyId, cancellationToken);
            if (project is null)
                return Array.Empty<CalendarEventDto>();

            var titleHint = project.Name;
            query = query.Where(x => x.Title.Contains(titleHint) || x.Type == "Project");
        }

        var rawRows = await query
            .OrderBy(x => x.StartDate)
            .Select(x => new CalendarEventDto
            {
                Id = x.Id,
                Title = x.Title,
                Type = x.Type,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Description = x.Description,
                AssignedToUserId = x.AssignedToUserId,
                Participants = x.Participants,
                CreatedById = x.CreatedById,
                LeadId = x.LeadId
            })
            .ToListAsync(cancellationToken);

        IEnumerable<CalendarEventDto> rows = rawRows;
        if (technicianId is { } tech)
        {
            rows = rawRows.Where(x =>
                x.AssignedToUserId == tech || x.Participants.Contains(tech));
        }

        var rowList = rows.ToList();

        // Admins: full society calendar (still bounded by from/to/project/technician filters above).
        if (callerUserId.HasValue && callerIsAdmin)
            return rowList;

        // Non-admin: commercial subtree visibility.
        if (callerUserId.HasValue)
        {
            return rowList
                .Where(x =>
                    (x.CreatedById.HasValue && relatedUserIds.Contains(x.CreatedById.Value))
                    || (x.AssignedToUserId.HasValue && relatedUserIds.Contains(x.AssignedToUserId.Value))
                    || x.Participants.Any(p => relatedUserIds.Contains(p)))
                .ToList();
        }

        return rowList;
    }

    public async Task<IReadOnlyList<CalendarEventDto>> ListEventsForSocietyUserAsync(
        Guid societyId,
        Guid targetUserId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var query = _app.Events.AsNoTracking().Where(x => x.SocietyId == societyId);
        if (from is { } f)
            query = query.Where(x => x.StartDate >= f);
        if (to is { } t)
            query = query.Where(x => x.StartDate <= t);

        var rawRows = await query
            .OrderBy(x => x.StartDate)
            .Select(x => new CalendarEventDto
            {
                Id = x.Id,
                Title = x.Title,
                Type = x.Type,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Description = x.Description,
                AssignedToUserId = x.AssignedToUserId,
                Participants = x.Participants,
                CreatedById = x.CreatedById,
                LeadId = x.LeadId
            })
            .ToListAsync(cancellationToken);

        return rawRows
            .Where(x => x.AssignedToUserId == targetUserId || x.Participants.Contains(targetUserId))
            .ToList();
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

    private async Task<bool> CanActorViewLeadAsync(
        Guid societyId,
        Guid leadId,
        Guid? callerUserId,
        bool callerIsAdmin,
        CancellationToken cancellationToken)
    {
        if (!callerUserId.HasValue)
            return false;

        if (callerIsAdmin)
        {
            return await _app.Leads.AsNoTracking()
                .AnyAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken);
        }

        var related = await GetRelatedUserIdsAsync(societyId, callerUserId.Value, cancellationToken);
        return await _app.Leads.AsNoTracking()
            .AnyAsync(
                x => x.Id == leadId && x.SocietyId == societyId
                     && ((x.CreatedById.HasValue && related.Contains(x.CreatedById.Value))
                         || (x.AssignedToUserId.HasValue && related.Contains(x.AssignedToUserId.Value))),
                cancellationToken);
    }
}
