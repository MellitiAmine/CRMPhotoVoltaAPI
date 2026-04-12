namespace CrmPhotoVolta.Application.Subscriptions.Dtos;

public sealed class CurrentSubscriptionDto
{
    public Guid SubscriptionId { get; init; }
    public Guid SocietyId { get; init; }
    public Guid PlanId { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class UpgradeSubscriptionRequest
{
    public Guid PlanId { get; init; }
}
