namespace CrmPhotoVolta.Application.Crm.Leads;

public sealed class LeadWonOrchestrationResult
{
    public Guid ClientId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid? QuoteId { get; init; }
    public bool ClientCreated { get; init; }
    public bool ProjectCreated { get; init; }
}
