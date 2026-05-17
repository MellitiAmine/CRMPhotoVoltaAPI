using CrmPhotoVolta.Application.Crm.Invoices;
using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Projects;

// ─── Nested reference DTOs ───────────────────────────────────────────────────

public sealed class ProjectUserRefDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
}

public sealed class ProjectClientRefDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
}

public sealed class ProjectLeadRefDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double? Lvi { get; init; }
    public double? Sd { get; init; }
}

public sealed class ProjectQuoteRefDto
{
    public Guid Id { get; init; }
    public string QuoteNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public QuoteStatus Status { get; init; }
    public decimal TotalTtc { get; init; }
    public DateTimeOffset? AcceptedAt { get; init; }
}

public sealed class ProjectTaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public CrmTaskStatus Status { get; init; }
    public LeadPriority Priority { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public string? AssignedToName { get; init; }
    public DateOnly? DueDate { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ProjectTimelineEventDto
{
    public Guid Id { get; init; }
    public ProjectTimelineEventType Type { get; init; }
    public string Description { get; init; } = string.Empty;
    public Guid? CreatedByUserId { get; init; }
    public string? CreatedByName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ProjectDocumentDto
{
    public Guid Id { get; init; }
    public ProjectDocumentType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public Guid? UploadedByUserId { get; init; }
    public string? UploadedByName { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}

public sealed class ContractSummaryDto
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public ContractType Type { get; init; }
    public ContractStatus Status { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTimeOffset? SignedAt { get; init; }
    public string? PdfUrl { get; init; }
}

public sealed class InvoiceSummaryDto
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public InvoiceStatus Status { get; init; }
    public DateOnly InvoiceDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public decimal TotalTtc { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public string? PdfUrl { get; init; }
}

public sealed class FinancialSummaryDto
{
    public decimal QuoteTotalTtc { get; init; }
    public decimal TotalInvoiced { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal TotalRemaining { get; init; }
    public decimal EstimatedMargin { get; init; }
    public decimal MarginPercent { get; init; }
    public bool FullyPaid { get; init; }
}

// ─── Main aggregate ───────────────────────────────────────────────────────────

public sealed class ProjectDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Reference { get; init; }
    public string? Address { get; init; }
    public ProjectStatus Status { get; init; }
    public LeadPriority Priority { get; init; }
    public string? Notes { get; init; }
    public int ProgressPercent { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateOnly? ExpectedInstallationDate { get; init; }
    public DateTimeOffset? LastActivityAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }

    // Solar sizing
    public decimal? SystemSizeKw { get; init; }
    public decimal? EstimatedProduction { get; init; }
    public string? RoofType { get; init; }
    public string? InstallationType { get; init; }
    public int? PanelCount { get; init; }
    public int? InverterCount { get; init; }

    // People
    public ProjectClientRefDto? Client { get; init; }
    public ProjectLeadRefDto? Lead { get; init; }
    public ProjectQuoteRefDto? Quote { get; init; }
    public ProjectUserRefDto? Commercial { get; init; }
    public ProjectUserRefDto? Technician { get; init; }
    public ProjectUserRefDto? Manager { get; init; }

    // Collections
    public IReadOnlyList<ProjectTaskDto> Tasks { get; init; } = Array.Empty<ProjectTaskDto>();
    public IReadOnlyList<ProjectTimelineEventDto> Timeline { get; init; } = Array.Empty<ProjectTimelineEventDto>();
    public IReadOnlyList<ProjectDocumentDto> Documents { get; init; } = Array.Empty<ProjectDocumentDto>();
    public IReadOnlyList<ContractSummaryDto> Contracts { get; init; } = Array.Empty<ContractSummaryDto>();
    public IReadOnlyList<InvoiceSummaryDto> Invoices { get; init; } = Array.Empty<InvoiceSummaryDto>();

    // Finance
    public FinancialSummaryDto Financial { get; init; } = null!;
}
