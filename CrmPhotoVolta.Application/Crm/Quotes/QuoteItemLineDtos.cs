namespace CrmPhotoVolta.Application.Crm.Quotes;

public sealed class CreateQuoteItemLineRequest
{
    public Guid QuoteId { get; init; }
    public Guid? ItemId { get; init; }
    public string? Description { get; init; }
    public decimal Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; }
    public decimal Discount { get; init; }
    public decimal? TvaRate { get; init; }
    public int SortOrder { get; init; }
}

public sealed class UpdateQuoteItemLineRequest
{
    public Guid? ItemId { get; init; }
    public string? Description { get; init; }
    public decimal Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; }
    public decimal Discount { get; init; }
    public decimal? TvaRate { get; init; }
    public int SortOrder { get; init; }
}
