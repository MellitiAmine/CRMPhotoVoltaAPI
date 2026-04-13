namespace CrmPhotoVolta.Application.Societies.Dtos;

public sealed class SocietyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? SubscriptionPlanId { get; init; }
    public string? SubscriptionPlanCode { get; init; }
    public string? SubscriptionPlanName { get; init; }
    public decimal SubscriptionPlanPrice { get; init; }
    public string SubscriptionPlanCurrency { get; init; } = "TND";
}
