namespace CrmPhotoVolta.Application.Crm.Items;

public sealed class ItemDto
{
    public Guid Id { get; init; }
    public Guid SocietyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Reference { get; init; }
    public string Unit { get; init; } = "piece";
    public decimal DefaultPrice { get; init; }
    public decimal TvaRate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateItemRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Reference { get; init; }
    public string Unit { get; init; } = "piece";
    public decimal DefaultPrice { get; init; }
    public decimal TvaRate { get; init; }
}
