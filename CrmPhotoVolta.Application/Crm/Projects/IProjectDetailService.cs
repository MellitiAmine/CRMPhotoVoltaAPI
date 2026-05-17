namespace CrmPhotoVolta.Application.Crm.Projects;

public interface IProjectDetailService
{
    Task<ProjectDetailDto> GetDetailAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default);
}
