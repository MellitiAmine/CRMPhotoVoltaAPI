namespace CrmPhotoVolta.Application.Scoring;

public interface ILeadScoringNotificationSink
{
    Task NotifyUserAsync(Guid societyId, Guid userId, string message, CancellationToken cancellationToken = default);
    Task NotifyManagerAsync(Guid societyId, string message, CancellationToken cancellationToken = default);

    /// <summary>Assigned commercial if set; otherwise first admin/manager (in-app notification).</summary>
    Task RouteCommercialOrAdminAsync(
        Guid societyId,
        Guid? assignedUserId,
        string title,
        string body,
        CancellationToken cancellationToken = default);
}
