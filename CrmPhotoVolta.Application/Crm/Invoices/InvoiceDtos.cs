using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Invoices;

public sealed class InvoiceItemDto
{
    public Guid Id { get; init; }
    public Guid? ItemId { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TvaRate { get; init; }
    public decimal TotalHt { get; init; }
}

public sealed class PaymentDto
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public DateOnly PaidOn { get; init; }
    public PaymentMethod Method { get; init; }
    public string? Reference { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class InvoiceDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public InvoiceStatus Status { get; init; }
    public DateOnly InvoiceDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public decimal TotalHt { get; init; }
    public decimal TotalTva { get; init; }
    public decimal TotalTtc { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public string? PdfUrl { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public IReadOnlyList<InvoiceItemDto> Items { get; init; } = Array.Empty<InvoiceItemDto>();
    public IReadOnlyList<PaymentDto> Payments { get; init; } = Array.Empty<PaymentDto>();
}

public sealed class CreateInvoiceItemRequest
{
    public Guid? ItemId { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; }
    public decimal TvaRate { get; init; } = 19;
}

public sealed class CreateInvoiceRequest
{
    public Guid ProjectId { get; init; }
    /// <summary>Optional — resolved from the project when omitted.</summary>
    public Guid? ClientId { get; init; }
    public string Reference { get; init; } = string.Empty;
    /// <summary>Defaults to today (UTC) when omitted.</summary>
    public DateOnly? InvoiceDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public string? Notes { get; init; }
    public string? PdfUrl { get; init; }
    public IReadOnlyList<CreateInvoiceItemRequest> Items { get; init; } = Array.Empty<CreateInvoiceItemRequest>();
}

public sealed class UpdateInvoiceRequest
{
    public InvoiceStatus Status { get; init; }
    public DateOnly? DueDate { get; init; }
    public string? Notes { get; init; }
    public string? PdfUrl { get; init; }
}

public sealed class AddPaymentRequest
{
    public decimal Amount { get; init; }
    public DateOnly PaidOn { get; init; }
    public PaymentMethod Method { get; init; } = PaymentMethod.BankTransfer;
    public string? Reference { get; init; }
    public string? Notes { get; init; }
}

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceDto>> ListByProjectAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default);

    Task<InvoiceDto> GetAsync(Guid societyId, Guid invoiceId, CancellationToken cancellationToken = default);

    Task<InvoiceDto> CreateAsync(Guid societyId, Guid actorUserId,
        CreateInvoiceRequest request, CancellationToken cancellationToken = default);

    Task<InvoiceDto> UpdateAsync(Guid societyId, Guid invoiceId,
        UpdateInvoiceRequest request, CancellationToken cancellationToken = default);

    Task<InvoiceDto> AddPaymentAsync(Guid societyId, Guid invoiceId, Guid actorUserId,
        AddPaymentRequest request, CancellationToken cancellationToken = default);

    Task<FinancialSummaryDto> GetFinancialSummaryAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default);
}
