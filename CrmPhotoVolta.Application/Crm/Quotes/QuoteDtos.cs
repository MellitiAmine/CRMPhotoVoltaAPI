namespace CrmPhotoVolta.Application.Crm.Quotes;

public sealed class QuoteItemDto
{
    public Guid Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public int SortOrder { get; init; }
}

public sealed class QuoteListItemDto
{
    public Guid Id { get; init; }
    public string QuoteNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "TND";
    public Guid? LeadId { get; init; }
    public Guid? ClientId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class QuoteDto
{
    public Guid Id { get; init; }
    public string QuoteNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Currency { get; init; } = "TND";
    public decimal TotalAmount { get; init; }
    public DateOnly? ValidUntil { get; init; }
    public Guid? LeadId { get; init; }
    public Guid? ClientId { get; init; }
    public Guid? DealId { get; init; }
    public Guid? ProjectId { get; init; }
    public DateTimeOffset? SentAt { get; init; }
    public DateTimeOffset? AcceptedAt { get; init; }
    public DateTimeOffset? RejectedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyList<QuoteItemDto> Items { get; init; } = Array.Empty<QuoteItemDto>();
}

public sealed class QuoteItemWriteDto
{
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; }
    public int SortOrder { get; init; }
}

public sealed class CreateQuoteRequest
{
    public Guid? LeadId { get; init; }
    public Guid? ClientId { get; init; }
    public Guid? DealId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Currency { get; init; } = "TND";
    public DateOnly? ValidUntil { get; init; }
    public IReadOnlyList<QuoteItemWriteDto> Items { get; init; } = Array.Empty<QuoteItemWriteDto>();
}

public sealed class UpdateQuoteRequest
{
    public string Title { get; init; } = string.Empty;
    public string Currency { get; init; } = "TND";
    public DateOnly? ValidUntil { get; init; }
    public Guid? LeadId { get; init; }
    public Guid? ClientId { get; init; }
    public Guid? DealId { get; init; }
    public IReadOnlyList<QuoteItemWriteDto> Items { get; init; } = Array.Empty<QuoteItemWriteDto>();
}

public sealed class ConvertQuoteToProjectRequest
{
    public string ProjectName { get; init; } = string.Empty;
    public string? Address { get; init; }
}
