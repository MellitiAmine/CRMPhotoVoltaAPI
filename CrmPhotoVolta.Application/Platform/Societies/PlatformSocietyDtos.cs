namespace CrmPhotoVolta.Application.Platform.Societies;

public sealed class CreatePlatformSocietyRequest
{
    public string Name { get; init; } = string.Empty;
    public Guid SubscriptionPlanId { get; init; }
}

public sealed class UpdatePlatformSocietyRequest
{
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public Guid? SubscriptionPlanId { get; init; }
}
