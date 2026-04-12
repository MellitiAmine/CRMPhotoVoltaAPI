using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data;
using CrmPhotoVolta.Infrastructure.Data.Core;
using CrmPhotoVolta.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Seeding;

public static class PlatformDemoSeeder
{
    /// <summary>Soft-deletes a legacy tenant <see cref="User"/> that used the same email as the platform operator.</summary>
    public static async Task RemoveLegacyTenantUserMatchingPlatformEmailAsync(
        CoreDbContext db,
        string platformAdminEmail,
        CancellationToken cancellationToken)
    {
        var email = platformAdminEmail.Trim().ToLowerInvariant();
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);

        if (user is null || user.IsDeleted)
            return;

        foreach (var us in await db.UserSocieties.Where(x => x.UserId == user.Id && !x.IsDeleted).ToListAsync(cancellationToken))
        {
            us.IsDeleted = true;
            us.UpdatedAt = DateTimeOffset.UtcNow;
        }

        foreach (var rt in await db.RefreshTokens
                     .IgnoreQueryFilters()
                     .Where(x => x.UserId == user.Id && x.RevokedAt == null && !x.IsDeleted)
                     .ToListAsync(cancellationToken))
        {
            rt.RevokedAt = DateTimeOffset.UtcNow;
            rt.UpdatedAt = DateTimeOffset.UtcNow;
        }

        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task SeedAsync(
        CoreDbContext core,
        PlatformSeedOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.Enabled || !options.CreateDemoSocieties)
            return;

        var freePlan = await core.SubscriptionPlans.AsNoTracking()
            .FirstAsync(x => x.Code == SubscriptionPlanCodes.FreeTrial3Months, cancellationToken);
        var paidPlan = await core.SubscriptionPlans.AsNoTracking()
            .FirstAsync(x => x.Code == SubscriptionPlanCodes.Standard100Monthly, cancellationToken);

        await EnsureDemoSocietyWithAdminUserAsync(
            core,
            options.DemoFreeSocietyAdminEmail,
            options.DemoFreeSocietyAdminPassword,
            options.DemoFreeSocietyAdminFullName,
            options.DemoSocietyFreeName,
            freePlan.Id,
            cancellationToken);

        await EnsureDemoSocietyWithAdminUserAsync(
            core,
            options.DemoPaidSocietyAdminEmail,
            options.DemoPaidSocietyAdminPassword,
            options.DemoPaidSocietyAdminFullName,
            options.DemoSocietyPaidName,
            paidPlan.Id,
            cancellationToken);
    }

    private static async Task EnsureDemoSocietyWithAdminUserAsync(
        CoreDbContext db,
        string adminEmail,
        string adminPassword,
        string adminFullName,
        string societyName,
        Guid planId,
        CancellationToken cancellationToken)
    {
        var plan = await db.SubscriptionPlans.AsNoTracking()
            .FirstAsync(x => x.Id == planId, cancellationToken);

        var society = await db.Societies
            .FirstOrDefaultAsync(x => x.Name == societyName && !x.IsDeleted, cancellationToken);

        if (society is null)
        {
            society = new Society
            {
                Name = societyName,
                SubscriptionPlanId = planId,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.Societies.Add(society);
            await db.SaveChangesAsync(cancellationToken);

            var adminRole = await RoleBootstrapper.CreateAdminRoleAsync(db, society.Id, cancellationToken);

            var adminUser = await EnsureTenantAdminUserAsync(db, adminEmail, adminPassword, adminFullName, cancellationToken);

            db.UserSocieties.Add(new UserSociety
            {
                UserId = adminUser.Id,
                SocietyId = society.Id,
                RoleId = adminRole.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });

            var start = DateOnly.FromDateTime(DateTime.UtcNow);
            db.Subscriptions.Add(new Subscription
            {
                SocietyId = society.Id,
                PlanId = planId,
                StartDate = start,
                EndDate = SubscriptionPeriodCalculator.ComputeEndDate(start, plan),
                Status = "Active",
                CreatedAt = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var tenantAdmin = await EnsureTenantAdminUserAsync(db, adminEmail, adminPassword, adminFullName, cancellationToken);
        var alreadyMember = await db.UserSocieties.AnyAsync(
            x => x.UserId == tenantAdmin.Id && x.SocietyId == society.Id && !x.IsDeleted,
            cancellationToken);

        if (alreadyMember)
            return;

        var societyAdminRole = await db.Roles
            .Where(x => x.SocietyId == society.Id && x.Name == "Admin" && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (societyAdminRole is null)
        {
            societyAdminRole = await RoleBootstrapper.CreateAdminRoleAsync(db, society.Id, cancellationToken);
        }

        db.UserSocieties.Add(new UserSociety
        {
            UserId = tenantAdmin.Id,
            SocietyId = society.Id,
            RoleId = societyAdminRole.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<User> EnsureTenantAdminUserAsync(
        CoreDbContext db,
        string email,
        string password,
        string fullName,
        CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var existing = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalized, cancellationToken);

        if (existing is not null)
            return existing;

        var user = new User
        {
            Email = normalized,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }
}
