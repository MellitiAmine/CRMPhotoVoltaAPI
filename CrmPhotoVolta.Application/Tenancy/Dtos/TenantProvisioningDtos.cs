using CrmPhotoVolta.Application.Subscriptions.Dtos;
using CrmPhotoVolta.Application.Users.Dtos;

namespace CrmPhotoVolta.Application.Tenancy.Dtos;

public sealed class CreateSocietyWithAdminRequest
{
    public string SocietyName { get; init; } = string.Empty;
    public Guid? SubscriptionPlanId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
    public string AdminFullName { get; init; } = string.Empty;
    public string? AdminPhone { get; init; }
    public string? AdminPassword { get; init; }
}

public sealed class AssignUserToSocietyRequest
{
    public Guid UserId { get; init; }
    public Guid SocietyId { get; init; }
    public Guid RoleId { get; init; }
}

public sealed class ActivateSubscriptionRequest
{
    public Guid SocietyId { get; init; }
    public Guid PlanId { get; init; }
}

public sealed class SocietyProvisioningResultDto
{
    public Guid SocietyId { get; init; }
    public UserDetailDto AdminUser { get; init; } = new();
    public CurrentSubscriptionDto Subscription { get; init; } = new();
}

