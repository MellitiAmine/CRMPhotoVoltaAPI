namespace CrmPhotoVolta.Domain.App;

public class InvoiceItem : SocietyScopedEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TvaRate { get; set; }
    public decimal TotalHt { get; set; }
}
