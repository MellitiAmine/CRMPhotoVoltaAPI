namespace CrmPhotoVolta.Domain.App;

public class QuoteItem : SocietyScopedEntity
{
    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    /// <summary>Discount percent (0–100).</summary>
    public decimal Discount { get; set; }
    /// <summary>VAT percent for this line.</summary>
    public decimal TvaRate { get; set; }
    /// <summary>Line total excluding VAT: Quantity × UnitPrice × (1 − Discount/100).</summary>
    public decimal TotalHt { get; set; }

    public int SortOrder { get; set; }
}
