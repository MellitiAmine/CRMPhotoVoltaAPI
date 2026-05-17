using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Projects;

public sealed class ChangeProjectStatusRequest
{
    public ProjectStatus Status { get; init; }
    public string? Comment { get; init; }
}

public interface IProjectWorkflowService
{
    Task<ProjectDto> ChangeStatusAsync(
        Guid societyId, Guid projectId, Guid actorUserId,
        ChangeProjectStatusRequest request,
        CancellationToken cancellationToken = default);
}
