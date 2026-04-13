using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Projects;

public sealed class ProjectListItemDto
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public Guid? DealId { get; init; }
    public string Name { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; }
    public decimal? SystemSizeKw { get; init; }
    public int ProgressPercent { get; init; }
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
    public ProjectStatus Status { get; init; }
    public decimal? SystemSizeKw { get; init; }
    public decimal? EstimatedProduction { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public Guid? ManagerUserId { get; init; }
    public Guid? TechnicianUserId { get; init; }
    public int ProgressPercent { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateProjectRequest
{
    public Guid ClientId { get; init; }
    public Guid? DealId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public ProjectStatus? Status { get; init; }
    public decimal? SystemSizeKw { get; init; }
    public decimal? EstimatedProduction { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public Guid? ManagerUserId { get; init; }
    public Guid? TechnicianUserId { get; init; }
    public int ProgressPercent { get; init; }
}

public sealed class UpdateProjectRequest
{
    public Guid ClientId { get; init; }
    public Guid? DealId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public ProjectStatus Status { get; init; }
    public decimal? SystemSizeKw { get; init; }
    public decimal? EstimatedProduction { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public Guid? ManagerUserId { get; init; }
    public Guid? TechnicianUserId { get; init; }
    public int ProgressPercent { get; init; }
}

public sealed class ProjectOverviewDto
{
    public ProjectDto Project { get; init; } = null!;
    public int OpenTasks { get; init; }
    public int InstallationsTotal { get; init; }
    public int InstallationsCompleted { get; init; }
}

public sealed class ProjectProgressDto
{
    public int ProgressPercent { get; init; }
    public int StageTrackingsCompleted { get; init; }
    public int StageTrackingsTotal { get; init; }
}

public sealed class AssignProjectUserRequest
{
    public Guid UserId { get; init; }
}

public sealed class PatchProjectProgressRequest
{
    public int ProgressPercent { get; init; }
}
