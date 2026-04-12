using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Platform;
using CrmPhotoVolta.Application.Platform.Dtos;
using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class PlatformSubscriptionPlanService : IPlatformSubscriptionPlanService
{
    private readonly CoreDbContext _db;

    public PlatformSubscriptionPlanService(CoreDbContext db)
    {
        _db = db;
    }

    public async Task<SubscriptionPlanAdminDto> CreateAsync(
        CreateSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new AppException("VALIDATION_ERROR", "Code is required.", 400);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        if (request.BillingPeriodMonths < 1)
            throw new AppException("VALIDATION_ERROR", "BillingPeriodMonths must be at least 1.", 400);

        if (request.MaxUsers < 1 || request.MaxProjects < 1)
            throw new AppException("VALIDATION_ERROR", "MaxUsers and MaxProjects must be at least 1.", 400);

        var code = request.Code.Trim().ToUpperInvariant();
        if (code.Length > 64)
            code = code[..64];

        if (await _db.SubscriptionPlans.AnyAsync(x => x.Code == code && !x.IsDeleted, cancellationToken))
            throw new AppException("CODE_IN_USE", "A plan with this code already exists.", 409);

        var cur = string.IsNullOrWhiteSpace(request.Currency) ? "TND" : request.Currency.Trim().ToUpperInvariant();
        cur = cur.Length > 8 ? cur[..8] : cur;

        var plan = new SubscriptionPlan
        {
            Code = code,
            Name = request.Name.Trim(),
            Currency = cur,
            Price = request.Price,
            TrialDurationMonths = request.TrialDurationMonths,
            BillingPeriodMonths = request.BillingPeriodMonths,
            MaxUsers = request.MaxUsers,
            MaxProjects = request.MaxProjects,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);

        return new SubscriptionPlanAdminDto
        {
            Id = plan.Id,
            Code = plan.Code,
            Name = plan.Name,
            Currency = plan.Currency,
            Price = plan.Price,
            TrialDurationMonths = plan.TrialDurationMonths,
            BillingPeriodMonths = plan.BillingPeriodMonths,
            MaxUsers = plan.MaxUsers,
            MaxProjects = plan.MaxProjects
        };
    }

    public async Task<IReadOnlyList<SubscriptionPlanAdminDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SubscriptionPlans
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Code)
            .Select(x => new SubscriptionPlanAdminDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Currency = x.Currency,
                Price = x.Price,
                TrialDurationMonths = x.TrialDurationMonths,
                BillingPeriodMonths = x.BillingPeriodMonths,
                MaxUsers = x.MaxUsers,
                MaxProjects = x.MaxProjects
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SubscriptionPlanAdminDto> UpdateAsync(
        Guid planId,
        UpdateSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == planId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("PLAN_NOT_FOUND", "Subscription plan not found.", 404);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        if (request.BillingPeriodMonths < 1)
            throw new AppException("VALIDATION_ERROR", "BillingPeriodMonths must be at least 1.", 400);

        if (request.MaxUsers < 1 || request.MaxProjects < 1)
            throw new AppException("VALIDATION_ERROR", "MaxUsers and MaxProjects must be at least 1.", 400);

        plan.Name = request.Name.Trim();
        var cur = string.IsNullOrWhiteSpace(request.Currency) ? "TND" : request.Currency.Trim().ToUpperInvariant();
        plan.Currency = cur.Length > 8 ? cur[..8] : cur;
        plan.Price = request.Price;
        plan.TrialDurationMonths = request.TrialDurationMonths;
        plan.BillingPeriodMonths = request.BillingPeriodMonths;
        plan.MaxUsers = request.MaxUsers;
        plan.MaxProjects = request.MaxProjects;
        plan.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new SubscriptionPlanAdminDto
        {
            Id = plan.Id,
            Code = plan.Code,
            Name = plan.Name,
            Currency = plan.Currency,
            Price = plan.Price,
            TrialDurationMonths = plan.TrialDurationMonths,
            BillingPeriodMonths = plan.BillingPeriodMonths,
            MaxUsers = plan.MaxUsers,
            MaxProjects = plan.MaxProjects
        };
    }
}
