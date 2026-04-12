namespace CrmPhotoVolta.Application.Crm.Documents;

public interface IDocumentService
{
    Task<DocumentDto> RegisterUploadAsync(Guid societyId, Guid? projectId, Guid? clientId, string type, string fileUrl, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentDto>> ListByProjectAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentDto>> ListByClientAsync(Guid societyId, Guid clientId, CancellationToken cancellationToken = default);
}

public sealed class DocumentDto
{
    public Guid Id { get; init; }
    public Guid? ProjectId { get; init; }
    public Guid? ClientId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public DateTimeOffset UploadedAt { get; init; }
}
