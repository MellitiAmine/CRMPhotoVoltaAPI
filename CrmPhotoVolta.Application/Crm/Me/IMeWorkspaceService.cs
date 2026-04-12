namespace CrmPhotoVolta.Application.Crm.Me;

public interface IMeWorkspaceService
{
    Task<IReadOnlyList<MyTaskDto>> GetMyTasksAsync(Guid societyId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MyInstallationDto>> GetMyInstallationsAsync(Guid societyId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MyScheduleEntryDto>> GetMyScheduleAsync(Guid societyId, Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}

public sealed class MyTaskDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateOnly? DueDate { get; init; }
}

public sealed class MyInstallationDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class MyScheduleEntryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
}
