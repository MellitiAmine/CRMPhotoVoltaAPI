using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Contracts;

public sealed class ContractDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public ContractType Type { get; init; }
    public ContractStatus Status { get; init; }
    public DateTimeOffset? SignedAt { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string? Notes { get; init; }
    public string? PdfUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateContractRequest
{
    public Guid ProjectId { get; init; }
    /// <summary>Optional — resolved from the project when omitted.</summary>
    public Guid? ClientId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public ContractType Type { get; init; } = ContractType.Installation;
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string? Notes { get; init; }
    public string? PdfUrl { get; init; }
}

public sealed class UpdateContractRequest
{
    public ContractStatus Status { get; init; }
    public DateTimeOffset? SignedAt { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string? Notes { get; init; }
    public string? PdfUrl { get; init; }
}

public interface IContractService
{
    Task<IReadOnlyList<ContractDto>> ListByProjectAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default);

    Task<ContractDto> GetAsync(Guid societyId, Guid contractId, CancellationToken cancellationToken = default);

    Task<ContractDto> CreateAsync(Guid societyId, Guid actorUserId,
        CreateContractRequest request, CancellationToken cancellationToken = default);

    Task<ContractDto> UpdateAsync(Guid societyId, Guid contractId,
        UpdateContractRequest request, CancellationToken cancellationToken = default);
}
