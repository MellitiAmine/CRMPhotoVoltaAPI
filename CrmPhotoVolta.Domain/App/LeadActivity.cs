namespace CrmPhotoVolta.Domain.App;

public class LeadActivity : SocietyScopedEntity
{
    public Guid LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public LeadActivityType Type { get; set; }
    public string? Notes { get; set; }

    /// <summary>Per-activity rating 1–5; null = not rated.</summary>
    public int? Rating { get; set; }

    public Guid CreatedByUserId { get; set; }
}
