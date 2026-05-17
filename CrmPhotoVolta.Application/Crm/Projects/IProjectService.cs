using CrmPhotoVolta.Application.Common;

namespace CrmPhotoVolta.Application.Crm.Projects;

public interface IProjectService
{
    Task<(IReadOnlyList<ProjectListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        Guid? clientId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    Task<ProjectDto> GetAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateAsync(Guid societyId, CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDto> UpdateAsync(Guid societyId, Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default);

    Task<ProjectOverviewDto> GetOverviewAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectProgressDto> GetProgressAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectDto> AssignTechnicianAsync(Guid societyId, Guid projectId, AssignProjectUserRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDto> AssignManagerAsync(Guid societyId, Guid projectId, AssignProjectUserRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDto> AssignCommercialAsync(Guid societyId, Guid projectId, AssignProjectUserRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDto> UpdateProgressAsync(Guid societyId, Guid projectId, PatchProjectProgressRequest request, CancellationToken cancellationToken = default);
}
