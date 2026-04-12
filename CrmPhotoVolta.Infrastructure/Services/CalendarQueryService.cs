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

        return await query
            .OrderBy(x => x.StartDate)
            .Select(x => new CalendarEventDto
            {
                Id = x.Id,
                Title = x.Title,
                Type = x.Type,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                AssignedToUserId = x.AssignedToUserId
            })
            .ToListAsync(cancellationToken);
    }
}
