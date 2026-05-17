using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Documents;

public sealed class UploadProjectDocumentRequest
{
    public ProjectDocumentType Type { get; init; } = ProjectDocumentType.Other;
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}

public interface IProjectDocumentService
{
    Task<IReadOnlyList<ProjectDocumentDto>> ListAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default);

    Task<ProjectDocumentDto> AddAsync(
        Guid societyId, Guid projectId, Guid uploaderUserId,
        UploadProjectDocumentRequest request, CancellationToken cancellationToken = default);
}
