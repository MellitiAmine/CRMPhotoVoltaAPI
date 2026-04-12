using CrmPhotoVolta.Application.Platform.Dtos;

namespace CrmPhotoVolta.Application.Platform;

public interface IPlatformSubscriptionPlanService
{
    Task<IReadOnlyList<SubscriptionPlanAdminDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<SubscriptionPlanAdminDto> CreateAsync(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken = default);
    Task<SubscriptionPlanAdminDto> UpdateAsync(Guid planId, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken = default);
}
