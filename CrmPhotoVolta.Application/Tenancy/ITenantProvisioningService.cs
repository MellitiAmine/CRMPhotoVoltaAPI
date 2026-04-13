using CrmPhotoVolta.Application.Subscriptions.Dtos;
using CrmPhotoVolta.Application.Tenancy.Dtos;
using CrmPhotoVolta.Application.Users.Dtos;

namespace CrmPhotoVolta.Application.Tenancy;

public interface ITenantProvisioningService
{
    Task<SocietyProvisioningResultDto> CreateSocietyWithAdminAsync(
        CreateSocietyWithAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<UserDetailDto> AssignUserToSocietyAsync(
        AssignUserToSocietyRequest request,
        CancellationToken cancellationToken = default);

    Task<CurrentSubscriptionDto> ActivateSubscriptionAsync(
        ActivateSubscriptionRequest request,
        CancellationToken cancellationToken = default);
}

