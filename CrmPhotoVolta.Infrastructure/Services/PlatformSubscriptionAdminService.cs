using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Platform.Subscriptions;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class PlatformSubscriptionAdminService : IPlatformSubscriptionAdminService
{
    private readonly CoreDbContext _db;

    public PlatformSubscriptionAdminService(CoreDbContext db)
    {
        _db = db;
    }

    public async Task<PlatformSubscriptionDto> UpdateAsync(
        Guid subscriptionId,
        UpdatePlatformSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            throw new AppException("VALIDATION_ERROR", "Status is required.", 400);

        var sub = await _db.Subscriptions
            .Include(x => x.Society)
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == subscriptionId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("SUBSCRIPTION_NOT_FOUND", "Subscription not found.", 404);

        sub.Status = request.Status.Trim();
        sub.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.EndDate is { } end)
            sub.EndDate = end;

        if (request.PlanId is { } planId)
        {
            var plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == planId && !x.IsDeleted, cancellationToken)
                ?? throw new AppException("PLAN_NOT_FOUND", "Subscription plan not found.", 404);

            sub.PlanId = plan.Id;
            if (sub.Society is not null)
            {
                sub.Society.SubscriptionPlanId = plan.Id;
                sub.Society.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var refreshed = await _db.Subscriptions.AsNoTracking()
            .Include(x => x.Plan)
            .FirstAsync(x => x.Id == subscriptionId, cancellationToken);

        return new PlatformSubscriptionDto
        {
            Id = refreshed.Id,
            SocietyId = refreshed.SocietyId,
            PlanId = refreshed.PlanId,
            PlanName = refreshed.Plan!.Name,
            StartDate = refreshed.StartDate,
            EndDate = refreshed.EndDate,
            Status = refreshed.Status
        };
    }
}
