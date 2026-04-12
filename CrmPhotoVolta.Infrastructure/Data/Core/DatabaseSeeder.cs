using CrmPhotoVolta.Domain.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Data.Core;

public static class DatabaseSeeder
{
    private static readonly PermissionSeed[] PermissionSeeds =
    {
        new("VIEW_PROJECT", "View projects"),
        new("MANAGE_PROJECT", "Manage projects"),
        new("VIEW_LEAD", "View leads"),
        new("MANAGE_LEAD", "Manage leads"),
        new("MANAGE_USERS", "Manage users"),
        new("MANAGE_ROLES", "Manage roles"),
        new("MANAGE_SOCIETY", "Manage society settings"),
        new("PLATFORM_MANAGE_SOCIETIES", "List and configure all societies and their subscription plans")
    };

    public static async Task EnsureSeedAsync(CoreDbContext db, CancellationToken cancellationToken)
    {
        await EnsurePermissionsAsync(db, cancellationToken);
        await EnsureSubscriptionPlansAsync(db, cancellationToken);
    }

    private static async Task EnsurePermissionsAsync(CoreDbContext db, CancellationToken cancellationToken)
    {
        foreach (var seed in PermissionSeeds)
        {
            if (await db.Permissions.AnyAsync(x => x.Code == seed.Code, cancellationToken))
                continue;

            db.Permissions.Add(new Permission
            {
                Code = seed.Code,
                Description = seed.Description,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSubscriptionPlansAsync(CoreDbContext db, CancellationToken cancellationToken)
    {
        await AlignLegacyStarterPlanAsync(db, cancellationToken);

        await UpsertPlanAsync(db, cancellationToken, new SubscriptionPlan
        {
            Code = SubscriptionPlanCodes.FreeTrial3Months,
            Name = "Essai gratuit — 3 mois",
            Currency = "TND",
            Price = 0,
            TrialDurationMonths = 3,
            BillingPeriodMonths = 3,
            MaxUsers = 5,
            MaxProjects = 20,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await UpsertPlanAsync(db, cancellationToken, new SubscriptionPlan
        {
            Code = SubscriptionPlanCodes.Standard100Monthly,
            Name = "Formule standard — 100 TND / mois",
            Currency = "TND",
            Price = 100,
            TrialDurationMonths = null,
            BillingPeriodMonths = 1,
            MaxUsers = 50,
            MaxProjects = 500,
            CreatedAt = DateTimeOffset.UtcNow
        });

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task AlignLegacyStarterPlanAsync(CoreDbContext db, CancellationToken cancellationToken)
    {
        var starter = await db.SubscriptionPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Name == "Starter", cancellationToken);

        if (starter is null)
            return;

        starter.Code = SubscriptionPlanCodes.FreeTrial3Months;
        starter.Name = "Essai gratuit — 3 mois";
        starter.Currency = "TND";
        starter.Price = 0;
        starter.TrialDurationMonths = 3;
        starter.BillingPeriodMonths = 3;
        starter.MaxUsers = 5;
        starter.MaxProjects = 20;
        starter.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static async Task UpsertPlanAsync(
        CoreDbContext db,
        CancellationToken cancellationToken,
        SubscriptionPlan template)
    {
        var existing = await db.SubscriptionPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Code == template.Code, cancellationToken);

        if (existing is null)
        {
            db.SubscriptionPlans.Add(template);
            return;
        }

        existing.Name = template.Name;
        existing.Currency = template.Currency;
        existing.Price = template.Price;
        existing.TrialDurationMonths = template.TrialDurationMonths;
        existing.BillingPeriodMonths = template.BillingPeriodMonths;
        existing.MaxUsers = template.MaxUsers;
        existing.MaxProjects = template.MaxProjects;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static async Task<SubscriptionPlan> GetDefaultRegistrationPlanAsync(
        CoreDbContext db,
        CancellationToken cancellationToken) =>
        await db.SubscriptionPlans.AsNoTracking()
            .FirstAsync(x => x.Code == SubscriptionPlanCodes.FreeTrial3Months, cancellationToken);

    private sealed record PermissionSeed(string Code, string Description);
}
