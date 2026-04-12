using CrmPhotoVolta.Application.Subscriptions.Dtos;

namespace CrmPhotoVolta.Application.Subscriptions;

public interface ISubscriptionService
{
    Task<CurrentSubscriptionDto?> GetCurrentAsync(Guid societyId, CancellationToken cancellationToken = default);
    Task<CurrentSubscriptionDto> UpgradeAsync(Guid societyId, UpgradeSubscriptionRequest request, CancellationToken cancellationToken = default);
}
