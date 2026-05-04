namespace CrmPhotoVolta.Application.Crm.Items;

public interface IItemService
{
    Task<ItemDto> CreateAsync(Guid societyId, CreateItemRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItemDto>> ListAsync(Guid societyId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid societyId, Guid itemId, CancellationToken cancellationToken = default);
}
