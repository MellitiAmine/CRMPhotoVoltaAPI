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
}
