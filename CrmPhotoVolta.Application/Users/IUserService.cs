using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Users.Dtos;

namespace CrmPhotoVolta.Application.Users;

public interface IUserService
{
    Task<(IReadOnlyList<UserListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    Task<UserDetailDto> GetAsync(Guid societyId, Guid userId, CancellationToken cancellationToken = default);
    Task<UserDetailDto> CreateAsync(Guid societyId, Guid actorUserId, CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDetailDto> UpdateAsync(Guid societyId, Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid societyId, Guid userId, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid societyId, Guid userId, AssignRoleRequest request, CancellationToken cancellationToken = default);
}
