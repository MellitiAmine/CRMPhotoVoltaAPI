using CrmPhotoVolta.Application.Common;

namespace CrmPhotoVolta.Application.Crm.Notifications;

public interface INotificationService
{
    Task<(IReadOnlyList<NotificationDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        Guid userId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    Task MarkReadAsync(Guid societyId, Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid societyId, Guid userId, CancellationToken cancellationToken = default);

    Task NotifyUserAsync(Guid societyId, Guid userId, string type, string title, string body, CancellationToken cancellationToken = default);

    Task MarkReadBatchAsync(Guid societyId, Guid userId, MarkNotificationsReadRequest request, CancellationToken cancellationToken = default);
}

public sealed class MarkNotificationsReadRequest
{
    public IReadOnlyList<Guid>? Ids { get; init; }
}

public sealed class NotificationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ReadAt { get; init; }
}
