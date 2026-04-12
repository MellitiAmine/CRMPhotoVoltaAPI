using CrmPhotoVolta.Application.Permissions.Dtos;
using CrmPhotoVolta.Application.Roles.Dtos;

namespace CrmPhotoVolta.Application.Roles;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> ListAsync(Guid societyId, CancellationToken cancellationToken = default);
    Task<RoleDto> CreateAsync(Guid societyId, CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateAsync(Guid societyId, Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid societyId, Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(Guid societyId, Guid roleId, CancellationToken cancellationToken = default);
    Task ReplacePermissionsAsync(Guid societyId, Guid roleId, ReplaceRolePermissionsRequest request, CancellationToken cancellationToken = default);
}
