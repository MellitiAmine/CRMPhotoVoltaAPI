namespace CrmPhotoVolta.Application.Crm.Leads;

/// <summary>Transactional workflow when a lead is marked won (client + project + timeline + tasks).</summary>
public interface ILeadWonOrchestrationService
{
    Task<LeadWonOrchestrationResult> ProcessAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
