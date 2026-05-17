using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Installations;

public interface IInstallationWorkflowService
{
    Task<(IReadOnlyList<InstallationListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        Guid? projectId,
        Guid? technicianId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallationListItemDto>> ListByProjectAsync(
        Guid societyId,
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<InstallationDto> GetAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken = default);

    Task<InstallationDto> CreateAsync(
        Guid societyId,
        CreateInstallationRequest request,
        CancellationToken cancellationToken = default);

    Task<InstallationDto> UpdateAsync(
        Guid societyId,
        Guid installationId,
        UpdateInstallationRequest request,
        CancellationToken cancellationToken = default);

    Task<InstallationDto> StartAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken = default);
    Task<InstallationDto> CompleteAsync(Guid societyId, Guid installationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstallationChecklistItemDto>> UpdateChecklistAsync(Guid societyId, Guid installationId, UpdateInstallationChecklistRequest request, CancellationToken cancellationToken = default);
    Task<InstallationPhotoDto> AddPhotoAsync(Guid societyId, Guid installationId, AddInstallationPhotoRequest request, CancellationToken cancellationToken = default);
}

public sealed class InstallationListItemDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string? ProjectReference { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public Guid TechnicianId { get; init; }
    public string? TechnicianName { get; init; }
    public DateOnly Date { get; init; }
    public InstallationStatus Status { get; init; }
    public int ChecklistCompleted { get; init; }
    public int ChecklistTotal { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class InstallationDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string? ProjectReference { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public Guid TechnicianId { get; init; }
    public string? TechnicianName { get; init; }
    public DateOnly Date { get; init; }
    public InstallationStatus Status { get; init; }
    public IReadOnlyList<InstallationChecklistItemDto> Checklist { get; init; } = Array.Empty<InstallationChecklistItemDto>();
    public IReadOnlyList<InstallationPhotoDto> Photos { get; init; } = Array.Empty<InstallationPhotoDto>();
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateInstallationRequest
{
    public Guid ProjectId { get; init; }
    public Guid TechnicianId { get; init; }
    public DateOnly Date { get; init; }
    public IReadOnlyList<string> DefaultChecklistItems { get; init; } = Array.Empty<string>();
}

public sealed class UpdateInstallationRequest
{
    public Guid TechnicianId { get; init; }
    public DateOnly Date { get; init; }
    public InstallationStatus? Status { get; init; }
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
