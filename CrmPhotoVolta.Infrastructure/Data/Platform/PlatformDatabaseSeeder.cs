using CrmPhotoVolta.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Data.Platform;

public static class PlatformDatabaseSeeder
{
    private static readonly (string Code, string Description)[] PermissionSeeds =
    {
        ("MANAGE_SOCIETIES", "Create and manage tenant societies"),
        ("MANAGE_SUBSCRIPTIONS", "Manage society subscriptions"),
        ("MANAGE_SUBSCRIPTION_PLANS", "Manage subscription plan catalog")
    };

    public static async Task EnsureSeedAsync(PlatformDbContext db, CancellationToken cancellationToken)
    {
        foreach (var (code, desc) in PermissionSeeds)
        {
            if (await db.PlatformPermissions.AnyAsync(x => x.Code == code, cancellationToken))
                continue;

            db.PlatformPermissions.Add(new PlatformPermission
            {
                Code = code,
                Description = desc,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken);

        var superAdminRole = await db.PlatformRoles.FirstOrDefaultAsync(x => x.Name == "SuperAdmin", cancellationToken);
        if (superAdminRole is null)
        {
            superAdminRole = new PlatformRole
            {
                Name = "SuperAdmin",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.PlatformRoles.Add(superAdminRole);
            await db.SaveChangesAsync(cancellationToken);
        }

        var allPerms = await db.PlatformPermissions.ToListAsync(cancellationToken);
        foreach (var p in allPerms)
        {
            if (await db.PlatformRolePermissions.AnyAsync(
                    x => x.PlatformRoleId == superAdminRole!.Id && x.PlatformPermissionId == p.Id,
                    cancellationToken))
                continue;

            db.PlatformRolePermissions.Add(new PlatformRolePermission
            {
                PlatformRoleId = superAdminRole.Id,
                PlatformPermissionId = p.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task EnsureSuperAdminUserAsync(
        PlatformDbContext db,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await db.PlatformUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);

        if (user is not null)
            return;

        var role = await db.PlatformRoles.FirstAsync(x => x.Name == "SuperAdmin", cancellationToken);

        user = new PlatformUser
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = "Super administrateur plateforme",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.PlatformUsers.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        db.PlatformUserRoles.Add(new PlatformUserRole
        {
            PlatformUserId = user.Id,
            PlatformRoleId = role.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
