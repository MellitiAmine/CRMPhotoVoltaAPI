using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Subscriptions;
using CrmPhotoVolta.Application.Subscriptions.Dtos;
using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class SubscriptionService : ISubscriptionService
{
    private readonly CoreDbContext _db;

    public SubscriptionService(CoreDbContext db)
    {
        _db = db;
    }

    public async Task<CurrentSubscriptionDto?> GetCurrentAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var row = await _db.Subscriptions
            .AsNoTracking()
            .Include(x => x.Plan)
            .Where(x => x.SocietyId == societyId && x.Status == "Active" && !x.IsDeleted)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        return new CurrentSubscriptionDto
        {
            SubscriptionId = row.Id,
            SocietyId = row.SocietyId,
            PlanId = row.PlanId,
            PlanName = row.Plan.Name,
            StartDate = row.StartDate,
            EndDate = row.EndDate,
            Status = row.Status
        };
    }

    public async Task<CurrentSubscriptionDto> UpgradeAsync(Guid societyId, UpgradeSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == request.PlanId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("PLAN_NOT_FOUND", "Subscription plan not found.", 404);

        var society = await _db.Societies.FirstOrDefaultAsync(x => x.Id == societyId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("SOCIETY_NOT_FOUND", "Society not found.", 404);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var sub in await _db.Subscriptions.Where(x => x.SocietyId == societyId && x.Status == "Active").ToListAsync(cancellationToken))
        {
            sub.Status = "Superseded";
            sub.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var newSub = new Subscription
        {
            SocietyId = societyId,
            PlanId = plan.Id,
            StartDate = today,
            EndDate = SubscriptionPeriodCalculator.ComputeEndDate(today, plan),
            Status = "Active",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Subscriptions.Add(newSub);
        society.SubscriptionPlanId = plan.Id;
        society.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return (await GetCurrentAsync(societyId, cancellationToken))!;
    }
}
