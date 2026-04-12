using CrmPhotoVolta.Application.Common;

namespace CrmPhotoVolta.Application.Crm.Clients;

public interface IClientService
{
    Task<(IReadOnlyList<ClientListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    Task<ClientDto> GetAsync(Guid societyId, Guid clientId, CancellationToken cancellationToken = default);
    Task<ClientDto> CreateAsync(Guid societyId, CreateClientRequest request, CancellationToken cancellationToken = default);
    Task<ClientDto> UpdateAsync(Guid societyId, Guid clientId, UpdateClientRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid societyId, Guid clientId, CancellationToken cancellationToken = default);
}
