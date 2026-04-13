using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

internal static class RoleBootstrapper
{
    public static async Task<Role> CreateAdminRoleAsync(CoreDbContext db, Guid societyId, CancellationToken cancellationToken)
    {
        var adminRole = await db.Roles
            .FirstOrDefaultAsync(x => x.SocietyId == societyId && x.Name == "Admin" && !x.IsDeleted, cancellationToken);
        if (adminRole is null)
        {
            adminRole = new Role
            {
                SocietyId = societyId,
                Name = "Admin",
                IsSystemRole = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.Roles.Add(adminRole);
            await db.SaveChangesAsync(cancellationToken);
        }

        var permissions = await db.Permissions.ToListAsync(cancellationToken);
        var existingPermissionIds = await db.RolePermissions
            .Where(x => x.RoleId == adminRole.Id)
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);

        foreach (var p in permissions)
        {
            if (existingPermissionIds.Contains(p.Id))
                continue;

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
