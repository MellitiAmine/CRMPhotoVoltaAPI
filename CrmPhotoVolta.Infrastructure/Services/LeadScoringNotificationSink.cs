using CrmPhotoVolta.Application.Crm.Notifications;
using CrmPhotoVolta.Application.Scoring;
using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class LeadScoringNotificationSink : ILeadScoringNotificationSink
{
    private readonly INotificationService _notifications;
    private readonly CoreDbContext _core;

    public LeadScoringNotificationSink(INotificationService notifications, CoreDbContext core)
    {
        _notifications = notifications;
        _core = core;
    }

    public Task NotifyUserAsync(
        Guid societyId,
        Guid userId,
        string message,
        CancellationToken cancellationToken = default) =>
        _notifications.NotifyUserAsync(societyId, userId, "LeadScoring", "Lead score", message, cancellationToken);

    public Task NotifyManagerAsync(Guid societyId, string message, CancellationToken cancellationToken = default) =>
        NotifyManagerInternalAsync(societyId, "Lead score", message, cancellationToken);

    public Task RouteCommercialOrAdminAsync(
        Guid societyId,
        Guid? assignedUserId,
        string title,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (assignedUserId is { } uid)
            return _notifications.NotifyUserAsync(societyId, uid, "LeadAutomation", title, body, cancellationToken);

        return NotifyManagerInternalAsync(societyId, title, body, cancellationToken);
    }

    private async Task NotifyManagerInternalAsync(
        Guid societyId,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        var recipientId = await ResolveFirstAdminOrManagerUserIdAsync(societyId, cancellationToken).ConfigureAwait(false);
        if (recipientId == Guid.Empty)
            return;

        await _notifications.NotifyUserAsync(societyId, recipientId, "LeadAutomation", title, body, cancellationToken)
            .ConfigureAwait(false);
    }

    private Task<Guid> ResolveFirstAdminOrManagerUserIdAsync(Guid societyId, CancellationToken cancellationToken) =>
        _core.UserSocieties.AsNoTracking()
            .Where(us => us.SocietyId == societyId && !us.IsDeleted)
            .Join(
                _core.Roles.AsNoTracking().Where(r => !r.IsDeleted && (r.RoleType == RoleType.Admin || r.RoleType == RoleType.Manager)),
                us => us.RoleId,
                r => r.Id,
                (us, _) => us.UserId)
            .OrderBy(userId => userId)
            .FirstOrDefaultAsync(cancellationToken);
}
