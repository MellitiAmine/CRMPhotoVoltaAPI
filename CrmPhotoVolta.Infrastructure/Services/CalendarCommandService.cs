using CrmPhotoVolta.Application.Crm.Calendar;
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

    public CalendarCommandService(AppDbContext app, CoreDbContext core)
    {
        _app = app;
        _core = core;
    }

    public async Task<CalendarEventDto> CreateAsync(
        Guid societyId,
        Guid creatorId,
        CreateCalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException("TITLE_REQUIRED", "Event title is required.", 400);

        if (request.EndDate <= request.StartDate)
            throw new AppException("INVALID_DATE_RANGE", "End date must be after start date.", 400);

        var validTypes = new[] { "meeting", "reminder", "activity" };
        if (!validTypes.Contains(request.Type.ToLowerInvariant()))
            throw new AppException("INVALID_EVENT_TYPE", "Type must be 'meeting', 'reminder', or 'activity'.", 400);

        // Validate that all participant IDs belong to the same society
        if (request.Participants.Count > 0)
        {
            var societyUserIds = await _core.UserSocieties
                .AsNoTracking()
                .Where(x => x.SocietyId == societyId && !x.IsDeleted)
                .Select(x => x.UserId)
                .ToListAsync(cancellationToken);

            var invalid = request.Participants.Except(societyUserIds).ToList();
            if (invalid.Count > 0)
                throw new AppException("INVALID_PARTICIPANT", "One or more participants do not belong to this society.", 400);
        }

        var ev = new CalendarEvent
        {
            SocietyId = societyId,
            Title = request.Title.Trim(),
            Type = request.Type.ToLowerInvariant(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Description = request.Description?.Trim(),
            Participants = request.Participants.Distinct().ToList(),
            AssignedToUserId = request.Participants.Count > 0 ? request.Participants[0] : null,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = creatorId
        };

        _app.Events.Add(ev);
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
            CreatedById = ev.CreatedById
        };
    }

    public async Task DeleteAsync(
        Guid societyId,
        Guid eventId,
        Guid requestingUserId,
        bool requestingUserIsAdmin,
        CancellationToken cancellationToken = default)
    {
        var ev = await _app.Events
            .FirstOrDefaultAsync(x => x.Id == eventId && x.SocietyId == societyId, cancellationToken);

        if (ev is null)
            throw new AppException("EVENT_NOT_FOUND", "Calendar event not found.", 404);

        // Only the creator or an admin may delete
        if (!requestingUserIsAdmin && ev.CreatedById != requestingUserId)
            throw new AppException("FORBIDDEN", "You are not allowed to delete this event.", 403);

        ev.IsDeleted = true;
        ev.UpdatedAt = DateTimeOffset.UtcNow;
        ev.UpdatedById = requestingUserId;

        await _app.SaveChangesAsync(cancellationToken);
    }
}
