using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Platform.Societies;
using CrmPhotoVolta.Application.Societies.Dtos;
using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class PlatformSocietyService : IPlatformSocietyService
{
    private readonly CoreDbContext _db;

    public PlatformSocietyService(CoreDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SocietyDto>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.Societies
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(x => Map(x, x.SubscriptionPlan));
    }

    public async Task<SocietyDto> GetAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var society = await _db.Societies
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
            .FirstOrDefaultAsync(x => x.Id == societyId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("SOCIETY_NOT_FOUND", "Society not found.", 404);

        return Map(society, society.SubscriptionPlan);
    }

    public async Task<SocietyDto> CreateAsync(CreatePlatformSocietyRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Society name is required.", 400);

        await DatabaseSeeder.EnsureSeedAsync(_db, cancellationToken);

        var plan = await _db.SubscriptionPlans.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.SubscriptionPlanId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("PLAN_NOT_FOUND", "Subscription plan not found.", 404);

        var society = new Society
        {
            Name = request.Name.Trim(),
            SubscriptionPlanId = plan.Id,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Societies.Add(society);
        await _db.SaveChangesAsync(cancellationToken);

        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        _db.Subscriptions.Add(new Subscription
        {
            SocietyId = society.Id,
            PlanId = plan.Id,
            StartDate = start,
            EndDate = SubscriptionPeriodCalculator.ComputeEndDate(start, plan),
            Status = "Active",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        var created = await _db.Societies.AsNoTracking()
            .Include(x => x.SubscriptionPlan)
            .FirstAsync(x => x.Id == society.Id, cancellationToken);

        return Map(created, created.SubscriptionPlan);
    }

    public async Task<SocietyDto> UpdateAsync(Guid societyId, UpdatePlatformSocietyRequest request, CancellationToken cancellationToken = default)
    {
        var society = await _db.Societies
            .Include(x => x.SubscriptionPlan)
            .FirstOrDefaultAsync(x => x.Id == societyId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("SOCIETY_NOT_FOUND", "Society not found.", 404);

        society.Name = request.Name.Trim();
        society.IsActive = request.IsActive;
        society.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.SubscriptionPlanId is { } newPlanId)
        {
            var newPlan = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == newPlanId && !x.IsDeleted, cancellationToken)
                ?? throw new AppException("PLAN_NOT_FOUND", "Subscription plan not found.", 404);

            society.SubscriptionPlanId = newPlan.Id;

            foreach (var sub in await _db.Subscriptions.Where(x => x.SocietyId == societyId && x.Status == "Active").ToListAsync(cancellationToken))
            {
                sub.Status = "Superseded";
                sub.UpdatedAt = DateTimeOffset.UtcNow;
            }

            var start = DateOnly.FromDateTime(DateTime.UtcNow);
            _db.Subscriptions.Add(new Subscription
            {
                SocietyId = societyId,
                PlanId = newPlan.Id,
                StartDate = start,
                EndDate = SubscriptionPeriodCalculator.ComputeEndDate(start, newPlan),
                Status = "Active",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var updated = await _db.Societies.AsNoTracking()
            .Include(x => x.SubscriptionPlan)
            .FirstAsync(x => x.Id == societyId, cancellationToken);

        return Map(updated, updated.SubscriptionPlan);
    }

    public async Task DeleteAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var society = await _db.Societies.FirstOrDefaultAsync(x => x.Id == societyId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("SOCIETY_NOT_FOUND", "Society not found.", 404);

        society.IsDeleted = true;
        society.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static SocietyDto Map(Society s, SubscriptionPlan? plan) => new()
    {
        Id = s.Id,
        Name = s.Name,
        IsActive = s.IsActive,
        CreatedAt = s.CreatedAt,
        SubscriptionPlanId = s.SubscriptionPlanId,
        SubscriptionPlanCode = plan?.Code,
        SubscriptionPlanName = plan?.Name,
        SubscriptionPlanPrice = plan?.Price ?? 0,
        SubscriptionPlanCurrency = plan?.Currency ?? "TND"
    };
}
