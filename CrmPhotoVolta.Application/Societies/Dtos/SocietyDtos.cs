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

public sealed class CreateSocietyRequest
{
    public string Name { get; init; } = string.Empty;

    /// <summary>Optional. Defaults to free trial (3 months) when omitted.</summary>
    public Guid? SubscriptionPlanId { get; init; }
}

public sealed class UpdateSocietyRequest
{
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}
