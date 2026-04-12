using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Societies;
using CrmPhotoVolta.Application.Societies.Dtos;
using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class SocietyService : ISocietyService
{
    private readonly CoreDbContext _db;

    public SocietyService(CoreDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SocietyDto>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var memberships = await _db.UserSocieties
            .AsNoTracking()
            .Include(x => x.Society!)
                .ThenInclude(s => s.SubscriptionPlan)
            .Where(x => x.UserId == userId && !x.IsDeleted && x.Society != null && !x.Society.IsDeleted)
            .OrderBy(x => x.Society!.Name)
            .ToListAsync(cancellationToken);

        return memberships.ConvertAll(x => Map(x.Society!, x.Society!.SubscriptionPlan));
    }

    public async Task<SocietyDto> GetAsync(Guid userId, Guid societyId, CancellationToken cancellationToken = default)
    {
        await EnsureAccessAsync(userId, societyId, cancellationToken);

        var society = await _db.Societies
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
            .FirstOrDefaultAsync(x => x.Id == societyId, cancellationToken)
            ?? throw new AppException("SOCIETY_NOT_FOUND", "Society not found.", 404);

        return Map(society, society.SubscriptionPlan);
    }

    public async Task<SocietyDto> CreateAsync(Guid userId, CreateSocietyRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Society name is required.", 400);

        await DatabaseSeeder.EnsureSeedAsync(_db, cancellationToken);

        SubscriptionPlan plan;
        if (request.SubscriptionPlanId is { } pid)
        {
            plan = await _db.SubscriptionPlans.AsNoTracking()
                       .FirstOrDefaultAsync(x => x.Id == pid && !x.IsDeleted, cancellationToken)
                   ?? throw new AppException("PLAN_NOT_FOUND", "Subscription plan not found.", 404);
        }
        else
        {
            plan = await DatabaseSeeder.GetDefaultRegistrationPlanAsync(_db, cancellationToken);
        }

        var society = new Society
        {
            Name = request.Name.Trim(),
            SubscriptionPlanId = plan.Id,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Societies.Add(society);
        await _db.SaveChangesAsync(cancellationToken);

        var adminRole = await RoleBootstrapper.CreateAdminRoleAsync(_db, society.Id, cancellationToken);

        _db.UserSocieties.Add(new UserSociety
        {
            UserId = userId,
            SocietyId = society.Id,
            RoleId = adminRole.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });

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

    public async Task<SocietyDto> UpdateAsync(Guid userId, Guid societyId, UpdateSocietyRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureAccessAsync(userId, societyId, cancellationToken);

        var society = await _db.Societies
            .Include(x => x.SubscriptionPlan)
            .FirstOrDefaultAsync(x => x.Id == societyId, cancellationToken)
            ?? throw new AppException("SOCIETY_NOT_FOUND", "Society not found.", 404);

        society.Name = request.Name.Trim();
        society.IsActive = request.IsActive;
        society.UpdatedAt = DateTimeOffset.UtcNow;
        society.UpdatedById = userId;

        await _db.SaveChangesAsync(cancellationToken);

        var updated = await _db.Societies.AsNoTracking()
            .Include(x => x.SubscriptionPlan)
            .FirstAsync(x => x.Id == societyId, cancellationToken);

        return Map(updated, updated.SubscriptionPlan);
    }

    public async Task DeleteAsync(Guid userId, Guid societyId, CancellationToken cancellationToken = default)
    {
        await EnsureAccessAsync(userId, societyId, cancellationToken);

        var society = await _db.Societies.FirstOrDefaultAsync(x => x.Id == societyId, cancellationToken)
            ?? throw new AppException("SOCIETY_NOT_FOUND", "Society not found.", 404);

        society.IsDeleted = true;
        society.UpdatedAt = DateTimeOffset.UtcNow;
        society.UpdatedById = userId;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAccessAsync(Guid userId, Guid societyId, CancellationToken cancellationToken)
    {
        var ok = await _db.UserSocieties.AnyAsync(
            x => x.UserId == userId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken);

        if (!ok)
            throw new AppException("FORBIDDEN", "You do not have access to this society.", 403);
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
