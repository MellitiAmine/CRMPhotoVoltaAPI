namespace CrmPhotoVolta.Application.Crm.Leads;

public sealed class LeadWonResultDto
{
    public LeadDto Lead { get; init; } = null!;
    public Guid ClientId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid? QuoteId { get; init; }
    public bool ClientCreated { get; init; }
    public bool ProjectCreated { get; init; }
}
