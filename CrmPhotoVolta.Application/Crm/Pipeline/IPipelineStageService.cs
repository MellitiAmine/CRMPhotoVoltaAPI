namespace CrmPhotoVolta.Application.Crm.Pipeline;

public interface IPipelineStageService
{
    Task<IReadOnlyList<PipelineStageDto>> ListAsync(Guid societyId, CancellationToken cancellationToken = default);
    Task<PipelineStageDto> CreateAsync(Guid societyId, CreatePipelineStageRequest request, CancellationToken cancellationToken = default);
    Task<PipelineStageDto> UpdateAsync(Guid societyId, Guid id, UpdatePipelineStageRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid societyId, Guid id, CancellationToken cancellationToken = default);
}

public sealed class PipelineStageDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Order { get; init; }
}

public sealed class CreatePipelineStageRequest
{
    public string Name { get; init; } = string.Empty;
    public int Order { get; init; }
}

public sealed class UpdatePipelineStageRequest
{
    public string Name { get; init; } = string.Empty;
    public int Order { get; init; }
}
