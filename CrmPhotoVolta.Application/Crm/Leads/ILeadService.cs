using CrmPhotoVolta.Application.Common;

namespace CrmPhotoVolta.Application.Crm.Leads;

public interface ILeadService
{
    Task<(IReadOnlyList<LeadListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    Task<LeadDto> GetAsync(Guid societyId, Guid leadId, CancellationToken cancellationToken = default);
    Task<LeadDto> CreateAsync(Guid societyId, Guid actorUserId, CreateLeadRequest request, CancellationToken cancellationToken = default);
    Task<LeadDto> UpdateAsync(Guid societyId, Guid leadId, UpdateLeadRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid societyId, Guid leadId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeadActivityDto>> ListActivitiesAsync(Guid societyId, Guid leadId, CancellationToken cancellationToken = default);
    Task<LeadActivityDto> AddActivityAsync(Guid societyId, Guid leadId, Guid actorUserId, AddLeadActivityRequest request, CancellationToken cancellationToken = default);

    Task<LeadDto> AssignAsync(Guid societyId, Guid leadId, Guid actorUserId, AssignLeadRequest request, CancellationToken cancellationToken = default);
    Task<ConvertLeadResultDto> ConvertAsync(Guid societyId, Guid leadId, Guid actorUserId, ConvertLeadRequest request, CancellationToken cancellationToken = default);
    Task<LeadDto> MarkWonAsync(Guid societyId, Guid leadId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LeadDto> MarkLostAsync(Guid societyId, Guid leadId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LeadActivityDto> AddNoteAsync(Guid societyId, Guid leadId, Guid actorUserId, AddLeadNoteRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeadTimelineEntryDto>> GetTimelineAsync(Guid societyId, Guid leadId, CancellationToken cancellationToken = default);

    /// <summary>Recalcule LVI/SD a partir des champs lead + activites (meme logique qu apres mise a jour).</summary>
    Task<LeadDto> RecalculateScoreAsync(Guid societyId, Guid leadId, CancellationToken cancellationToken = default);

    /// <summary>Change le statut manuellement et applique le score minimum associe au statut.</summary>
    Task<LeadDto> ChangeStatusAsync(Guid societyId, Guid leadId, Guid actorUserId, ChangeLeadStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change la temperature manuellement (Hot/High/Medium/Low/Cold).
    /// Applique le score minimum correspondant et trace UpdatedById / UpdatedAt.
    /// </summary>
    Task<LeadDto> ChangeTemperatureAsync(Guid societyId, Guid leadId, Guid actorUserId, ChangeLeadTemperatureRequest request, CancellationToken cancellationToken = default);

    /// <summary>Ajoute un tag manuel au lead.</summary>
    Task<LeadDto> AddTagAsync(Guid societyId, Guid leadId, AddLeadTagRequest request, CancellationToken cancellationToken = default);

    /// <summary>Supprime un tag manuel du lead.</summary>
    Task<LeadDto> RemoveTagAsync(Guid societyId, Guid leadId, string tag, CancellationToken cancellationToken = default);
}
