using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Documents;

public interface IProjectDocumentService
{
    Task<IReadOnlyList<ProjectDocumentDto>> ListAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default);

    Task<ProjectDocumentDto> UploadAsync(
        Guid societyId,
        Guid projectId,
        Guid uploaderUserId,
        ProjectDocumentType type,
        string? displayName,
        string fileName,
        string contentType,
        long length,
        Stream content,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid societyId, Guid projectId, Guid documentId, CancellationToken cancellationToken = default);
}
