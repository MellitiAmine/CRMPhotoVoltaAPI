namespace CrmPhotoVolta.Domain.App;

public class LeadActivity : SocietyScopedEntity
{
    public Guid LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public string Type { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
}
