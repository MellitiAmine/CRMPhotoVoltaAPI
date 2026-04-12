using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Notifications;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class CrmNotificationService : INotificationService
{
    private readonly AppDbContext _app;

    public CrmNotificationService(AppDbContext app)
    {
        _app = app;
    }

    public async Task<(IReadOnlyList<NotificationDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        Guid userId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _app.Notifications.AsNoTracking()
            .Where(x => x.SocietyId == societyId && x.UserId == userId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Title = x.Title,
                Body = x.Body,
                Type = x.Type,
                CreatedAt = x.CreatedAt,
                ReadAt = x.ReadAt
            })
            .ToListAsync(cancellationToken);

        return (items, pagination.ToMeta(total));
    }

    public async Task MarkReadAsync(Guid societyId, Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var row = await _app.Notifications.FirstOrDefaultAsync(
            x => x.Id == notificationId && x.SocietyId == societyId && x.UserId == userId,
            cancellationToken);

        if (row is null)
            return;

        row.ReadAt = DateTimeOffset.UtcNow;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid societyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var rows = await _app.Notifications
            .Where(x => x.SocietyId == societyId && x.UserId == userId && x.ReadAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var r in rows)
        {
            r.ReadAt = now;
            r.UpdatedAt = now;
        }

        if (rows.Count > 0)
            await _app.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyUserAsync(
        Guid societyId,
        Guid userId,
        string type,
        string title,
        string body,
        CancellationToken cancellationToken = default)
    {
        _app.Notifications.Add(new Notification
        {
            SocietyId = societyId,
            UserId = userId,
            Type = type.Trim(),
            Title = title.Trim(),
            Body = body.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _app.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkReadBatchAsync(
        Guid societyId,
        Guid userId,
        MarkNotificationsReadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Ids is null || request.Ids.Count == 0)
        {
            await MarkAllReadAsync(societyId, userId, cancellationToken);
            return;
        }

        foreach (var id in request.Ids.Distinct())
            await MarkReadAsync(societyId, userId, id, cancellationToken);
    }
}
