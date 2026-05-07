using CrmPhotoVolta.Application.Crm.Calendar;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class CalendarQueryService : ICalendarQueryService
{
    private readonly AppDbContext _app;

    public CalendarQueryService(AppDbContext app)
    {
        _app = app;
    }

    public async Task<IReadOnlyList<CalendarEventDto>> ListAsync(
        Guid societyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        Guid? technicianId,
        Guid? projectId,
        Guid? callerUserId,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        var query = _app.Events.AsNoTracking().Where(x => x.SocietyId == societyId);

        if (from is { } f)
            query = query.Where(x => x.StartDate >= f);
        if (to is { } t)
            query = query.Where(x => x.StartDate <= t);
        if (technicianId is { } tech)
            query = query.Where(x => x.AssignedToUserId == tech);

        if (projectId is { } pid)
        {
            var project = await _app.Projects.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == pid && x.SocietyId == societyId, cancellationToken);
            if (project is null)
                return Array.Empty<CalendarEventDto>();

            var titleHint = project.Name;
            query = query.Where(x => x.Title.Contains(titleHint) || x.Type == "Project");
        }

        // Non-admin users only see events they created or are a participant in
        if (!callerIsAdmin && callerUserId.HasValue)
        {
            var uid = callerUserId.Value;
            // EF Core can't serialize List<Guid> to SQL LIKE easily; load filtered set in memory.
            // We apply a broad filter then refine in memory to keep the query sane.
            var raw = await query
                .OrderBy(x => x.StartDate)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Type,
                    x.StartDate,
                    x.EndDate,
                    x.Description,
                    x.AssignedToUserId,
                    x.Participants,
                    x.CreatedById,
                    x.LeadId
                })
                .ToListAsync(cancellationToken);

            return raw
                .Where(x => x.CreatedById == uid || x.Participants.Contains(uid))
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
                .ToList();
        }

        return await query
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
    }
}
