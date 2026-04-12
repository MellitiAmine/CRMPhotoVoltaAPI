using CrmPhotoVolta.Application.Permissions.Dtos;

namespace CrmPhotoVolta.Application.Permissions;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionDto>> ListAsync(CancellationToken cancellationToken = default);
}
