namespace CrmPhotoVolta.Application.Platform.Dtos;

public sealed class SubscriptionPlanAdminDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "TND";
    public decimal Price { get; init; }
    public int? TrialDurationMonths { get; init; }
    public int BillingPeriodMonths { get; init; }
    public int MaxUsers { get; init; }
    public int MaxProjects { get; init; }
}

public sealed class CreateSubscriptionPlanRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "TND";
    public decimal Price { get; init; }
    public int? TrialDurationMonths { get; init; }
    public int BillingPeriodMonths { get; init; } = 1;
    public int MaxUsers { get; init; }
    public int MaxProjects { get; init; }
}

public sealed class UpdateSubscriptionPlanRequest
{
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "TND";
    public decimal Price { get; init; }
    public int? TrialDurationMonths { get; init; }
    public int BillingPeriodMonths { get; init; } = 1;
    public int MaxUsers { get; init; }
    public int MaxProjects { get; init; }
}
