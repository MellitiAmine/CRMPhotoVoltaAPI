namespace CrmPhotoVolta.Application.Crm.Leads;

/// <summary>Stages journal rows on <see cref="CrmPhotoVolta.Infrastructure.Data.App.AppDbContext"/>; callers must <c>SaveChanges</c>.</summary>
public interface ILeadJournalService
{
    void Stage(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        string action,
        string? relatedEntityType,
        Guid? relatedEntityId,
        object? metadata);

    Task<IReadOnlyList<LeadJournalEntryDto>> ListForLeadAsync(
        Guid societyId,
        Guid leadId,
        CancellationToken cancellationToken = default);
}
