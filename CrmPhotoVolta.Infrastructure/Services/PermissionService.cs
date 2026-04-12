using CrmPhotoVolta.Application.Permissions;
using CrmPhotoVolta.Application.Permissions.Dtos;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class PermissionService : IPermissionService
{
    private readonly CoreDbContext _db;

    public PermissionService(CoreDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PermissionDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new PermissionDto
            {
                Id = x.Id,
                Code = x.Code,
                Description = x.Description
            })
            .ToListAsync(cancellationToken);
    }
}
