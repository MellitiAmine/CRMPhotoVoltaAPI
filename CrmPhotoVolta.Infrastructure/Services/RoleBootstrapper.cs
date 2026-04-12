using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

internal static class RoleBootstrapper
{
    public static async Task<Role> CreateAdminRoleAsync(CoreDbContext db, Guid societyId, CancellationToken cancellationToken)
    {
        var adminRole = new Role
        {
            SocietyId = societyId,
            Name = "Admin",
            IsSystemRole = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Roles.Add(adminRole);
        await db.SaveChangesAsync(cancellationToken);

        var permissions = await db.Permissions.ToListAsync(cancellationToken);
        foreach (var p in permissions)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = p.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return adminRole;
    }
}
