namespace CrmPhotoVolta.Application.Crm.Installations;

public interface IInstallationWorkflowService
{
    Task<InstallationDto> GetAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken = default);
    Task<InstallationDto> StartAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken = default);
    Task<InstallationDto> CompleteAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstallationChecklistItemDto>> UpdateChecklistAsync(Guid societyId, Guid installationId, UpdateInstallationChecklistRequest request, CancellationToken cancellationToken = default);
    Task<InstallationPhotoDto> AddPhotoAsync(Guid societyId, Guid installationId, AddInstallationPhotoRequest request, CancellationToken cancellationToken = default);
}

public sealed class InstallationDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid TechnicianId { get; init; }
    public DateOnly Date { get; init; }
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<InstallationChecklistItemDto> Checklist { get; init; } = Array.Empty<InstallationChecklistItemDto>();
    public IReadOnlyList<InstallationPhotoDto> Photos { get; init; } = Array.Empty<InstallationPhotoDto>();
}

public sealed class InstallationChecklistItemDto
{
    public Guid Id { get; init; }
    public string Item { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
}

public sealed class InstallationPhotoDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public DateTimeOffset UploadedAt { get; init; }
}

public sealed class ChecklistItemUpdateDto
{
    public Guid Id { get; init; }
    public bool IsCompleted { get; init; }
}

public sealed class UpdateInstallationChecklistRequest
{
    public IReadOnlyList<ChecklistItemUpdateDto> Items { get; init; } = Array.Empty<ChecklistItemUpdateDto>();
}

public sealed class AddInstallationPhotoRequest
{
    public string Url { get; init; } = string.Empty;
}
