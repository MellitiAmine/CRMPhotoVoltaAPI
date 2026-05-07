using CrmPhotoVolta.Application.Crm.Me;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class MeWorkspaceService : IMeWorkspaceService
{
    private readonly AppDbContext _app;

    public MeWorkspaceService(AppDbContext app)
    {
        _app = app;
    }

    public async Task<IReadOnlyList<MyTaskDto>> GetMyTasksAsync(Guid societyId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _app.Tasks.AsNoTracking()
            .Include(x => x.Project)
            .Where(x => x.SocietyId == societyId && x.AssignedToUserId == userId)
            .OrderBy(x => x.DueDate)
            .Select(x => new MyTaskDto
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                ProjectName = x.Project!.Name,
                Title = x.Title,
                Status = x.Status,
                DueDate = x.DueDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MyInstallationDto>> GetMyInstallationsAsync(Guid societyId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _app.Installations.AsNoTracking()
            .Include(x => x.Project)
            .Where(x => x.SocietyId == societyId && x.TechnicianId == userId)
            .OrderByDescending(x => x.Date)
            .Select(x => new MyInstallationDto
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                ProjectName = x.Project!.Name,
                Date = x.Date,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MyScheduleEntryDto>> GetMyScheduleAsync(
        Guid societyId,
        Guid userId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var raw = await _app.Events.AsNoTracking()
            .Where(x =>
                x.SocietyId == societyId &&
                x.StartDate >= from &&
                x.StartDate <= to)
            .OrderBy(x => x.StartDate)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Type,
                x.StartDate,
                x.EndDate,
                x.AssignedToUserId,
                x.Participants,
                x.LeadId
            })
            .ToListAsync(cancellationToken);

        return raw
            .Where(x => x.AssignedToUserId == userId || x.Participants.Contains(userId))
            .Select(x => new MyScheduleEntryDto
            {
                Id = x.Id,
                Title = x.Title,
                Type = x.Type,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                LeadId = x.LeadId
            })
            .ToList();
    }
}
