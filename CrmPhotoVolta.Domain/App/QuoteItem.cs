namespace CrmPhotoVolta.Domain.App;

public class QuoteItem : SocietyScopedEntity
{
    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public int SortOrder { get; set; }
}
