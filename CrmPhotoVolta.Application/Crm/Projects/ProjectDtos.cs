namespace CrmPhotoVolta.Application.Crm.Projects;

public sealed class ProjectListItemDto
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public Guid? DealId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal? SystemSizeKw { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ProjectDto
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public Guid? DealId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal? SystemSizeKw { get; init; }
    public decimal? EstimatedProduction { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateProjectRequest
{
    public Guid ClientId { get; init; }
    public Guid? DealId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Status { get; init; }
    public decimal? SystemSizeKw { get; init; }
    public decimal? EstimatedProduction { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
}

public sealed class UpdateProjectRequest
{
    public Guid ClientId { get; init; }
    public Guid? DealId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal? SystemSizeKw { get; init; }
    public decimal? EstimatedProduction { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
}
