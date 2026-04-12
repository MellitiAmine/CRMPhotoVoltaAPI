namespace CrmPhotoVolta.Application.Platform.Subscriptions;

public sealed class UpdatePlatformSubscriptionRequest
{
    public string Status { get; init; } = string.Empty;
    public DateOnly? EndDate { get; init; }
    public Guid? PlanId { get; init; }
}

public sealed class PlatformSubscriptionDto
{
    public Guid Id { get; init; }
    public Guid SocietyId { get; init; }
    public Guid PlanId { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public string Status { get; init; } = string.Empty;
}
