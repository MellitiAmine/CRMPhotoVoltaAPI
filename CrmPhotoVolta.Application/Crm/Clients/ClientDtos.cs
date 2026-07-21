using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Clients;

public sealed class ClientListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public Guid? UserId { get; init; }
    public bool IsActive { get; init; }
    public int ProjectCount { get; init; }
    public int ActiveProjectCount { get; init; }
    public decimal TotalInvoicedTtc { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal TotalRemaining { get; init; }
    public DateTimeOffset? LastActivityAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ClientDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public Guid? UserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class Client360SummaryDto
{
    public bool IsActive { get; init; }
    public int ProjectCount { get; init; }
    public int ActiveProjectCount { get; init; }
    public int InstallationCount { get; init; }
    public int InvoiceCount { get; init; }
    public int PaymentCount { get; init; }
    public decimal TotalInvoicedTtc { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal TotalRemaining { get; init; }
    public DateTimeOffset? LastActivityAt { get; init; }
}

public sealed class Client360ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Reference { get; init; }
    public ProjectStatus Status { get; init; }
    public decimal TotalTtc { get; init; }
    public int ProgressPercent { get; init; }
    public string? CommercialName { get; init; }
    public string? TechnicianName { get; init; }
    public DateTimeOffset? LastActivityAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class Client360InvoiceDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public string Reference { get; init; } = string.Empty;
    public InvoiceStatus Status { get; init; }
    public DateOnly InvoiceDate { get; init; }
    public decimal TotalTtc { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
}

public sealed class Client360InstallationDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public Guid TechnicianId { get; init; }
    public string? TechnicianName { get; init; }
    public DateOnly Date { get; init; }
    public InstallationStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class Client360PaymentDto
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public string InvoiceReference { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public decimal Amount { get; init; }
    public DateOnly PaidOn { get; init; }
    public PaymentMethod Method { get; init; }
    public string? Reference { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class Client360Dto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public Guid? UserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public Client360SummaryDto Summary { get; init; } = new();
    public IReadOnlyList<Client360ProjectDto> Projects { get; init; } = Array.Empty<Client360ProjectDto>();
    public IReadOnlyList<Client360InvoiceDto> Invoices { get; init; } = Array.Empty<Client360InvoiceDto>();
    public IReadOnlyList<Client360InstallationDto> Installations { get; init; } = Array.Empty<Client360InstallationDto>();
    public IReadOnlyList<Client360PaymentDto> Payments { get; init; } = Array.Empty<Client360PaymentDto>();
}

public sealed record ClientListQuery(
    string? Search = null,
    string? Activity = null,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string SortOrder = "desc");

public sealed class CreateClientRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public Guid? UserId { get; init; }
}

public sealed class UpdateClientRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public Guid? UserId { get; init; }
}
