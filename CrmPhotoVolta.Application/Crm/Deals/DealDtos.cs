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
