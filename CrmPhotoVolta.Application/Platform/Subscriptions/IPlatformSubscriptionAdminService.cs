namespace CrmPhotoVolta.Application.Platform.Subscriptions;

public interface IPlatformSubscriptionAdminService
{
    Task<PlatformSubscriptionDto> UpdateAsync(Guid subscriptionId, UpdatePlatformSubscriptionRequest request, CancellationToken cancellationToken = default);
}
