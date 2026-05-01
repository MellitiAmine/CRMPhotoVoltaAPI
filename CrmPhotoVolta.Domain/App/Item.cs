namespace CrmPhotoVolta.Domain.App;

/// <summary>Catalog article (multi-tenant via <see cref="SocietyScopedEntity.SocietyId"/>).</summary>
public class Item : SocietyScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Reference { get; set; }
    /// <summary>Unit of measure, e.g. piece, meter.</summary>
    public string Unit { get; set; } = "piece";

    public decimal DefaultPrice { get; set; }
    /// <summary>VAT rate percent (e.g. 7, 19).</summary>
    public decimal TvaRate { get; set; }
}
