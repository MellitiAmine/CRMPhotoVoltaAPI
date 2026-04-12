using CrmPhotoVolta.Application.Common;

namespace CrmPhotoVolta.Application.Crm.Deals;

public interface IDealService
{
    Task<(IReadOnlyList<DealListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    Task<DealDto> GetAsync(Guid societyId, Guid dealId, CancellationToken cancellationToken = default);
    Task<DealDto> CreateAsync(Guid societyId, CreateDealRequest request, CancellationToken cancellationToken = default);
    Task<DealDto> UpdateAsync(Guid societyId, Guid dealId, UpdateDealRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid societyId, Guid dealId, CancellationToken cancellationToken = default);
}
