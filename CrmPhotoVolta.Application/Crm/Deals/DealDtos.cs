using CrmPhotoVolta.Application.Crm.Leads;
using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Deals;

public sealed class DealListItemDto
{
    public Guid Id { get; init; }
    public Guid? LeadId { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal? Value { get; init; }
    public string Stage { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class DealDto
{
    public Guid Id { get; init; }
    public Guid? LeadId { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal? Value { get; init; }
    public string Stage { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DealLeadInfoDto? LeadInfo { get; init; }
}

public sealed class DealLeadInfoDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double? Lvi { get; init; }
    public double? Sd { get; init; }
    public DateTimeOffset? ScoredAt { get; init; }
    public LeadTemperature Temperature { get; init; } = LeadTemperature.Cold;
    public LeadPriority Priority { get; init; } = LeadPriority.Low;
    public LeadScoreBreakdownDto? ScoreBreakdown { get; init; }
}

public sealed class CreateDealRequest
{
    public Guid? LeadId { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal? Value { get; init; }
    public string? Stage { get; init; }
    public Guid? AssignedToUserId { get; init; }
}

public sealed class UpdateDealRequest
{
    public Guid? LeadId { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal? Value { get; init; }
    public string Stage { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
}
