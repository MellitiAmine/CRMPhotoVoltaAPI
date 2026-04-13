using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Subscriptions.Dtos;
using CrmPhotoVolta.Application.Tenancy;
using CrmPhotoVolta.Application.Tenancy.Dtos;
using CrmPhotoVolta.Application.Users.Dtos;
using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class TenantProvisioningService : ITenantProvisioningService
{
    private readonly CoreDbContext _db;

    public TenantProvisioningService(CoreDbContext db)
    {
        _db = db;
    }

    public async Task<SocietyProvisioningResultDto> CreateSocietyWithAdminAsync(
        CreateSocietyWithAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SocietyName))
            throw new AppException("VALIDATION_ERROR", "Society name is required.", 400);
        if (string.IsNullOrWhiteSpace(request.AdminEmail))
            throw new AppException("VALIDATION_ERROR", "Admin email is required.", 400);
        if (string.IsNullOrWhiteSpace(request.AdminFullName))
            throw new AppException("VALIDATION_ERROR", "Admin full name is required.", 400);

        await DatabaseSeeder.EnsureSeedAsync(_db, cancellationToken);

        var normalizedEmail = request.AdminEmail.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Email.ToLower() == normalizedEmail && !x.IsDeleted, cancellationToken))
            throw new AppException("EMAIL_IN_USE", "Admin email is already used by an existing user.", 409);

        SubscriptionPlan plan;
        if (request.SubscriptionPlanId is { } planId)
        {
            plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == planId && !x.IsDeleted, cancellationToken)
                ?? throw new AppException("PLAN_NOT_FOUND", "Subscription plan not found.", 404);
        }
        else
        {
            plan = await DatabaseSeeder.GetDefaultRegistrationPlanAsync(_db, cancellationToken);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var society = new Society
        {
            Name = request.SocietyName.Trim(),
            IsActive = true,
            SubscriptionPlanId = plan.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Societies.Add(society);
        await _db.SaveChangesAsync(cancellationToken);

        var adminRole = await RoleBootstrapper.CreateAdminRoleAsync(_db, society.Id, cancellationToken);

        var user = new User
        {
            Email = normalizedEmail,
            FullName = request.AdminFullName.Trim(),
            Phone = request.AdminPhone?.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(string.IsNullOrWhiteSpace(request.AdminPassword) ? "ChangeMe123!" : request.AdminPassword),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        _db.UserSocieties.Add(new UserSociety
        {
            UserId = user.Id,
            SocietyId = society.Id,
            RoleId = adminRole.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var subscription = new Subscription
        {
            SocietyId = society.Id,
            PlanId = plan.Id,
            StartDate = startDate,
            EndDate = SubscriptionPeriodCalculator.ComputeEndDate(startDate, plan),
            Status = SubscriptionStatuses.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Subscriptions.Add(subscription);

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new SocietyProvisioningResultDto
        {
            SocietyId = society.Id,
            AdminUser = new UserDetailDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                IsActive = user.IsActive,
                RoleId = adminRole.Id,
                RoleName = adminRole.Name,
                CreatedAt = user.CreatedAt
            },
            Subscription = new CurrentSubscriptionDto
            {
                SubscriptionId = subscription.Id,
                SocietyId = society.Id,
                PlanId = plan.Id,
                PlanName = plan.Name,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                Status = subscription.Status
            }
        };
    }

    public async Task<UserDetailDto> AssignUserToSocietyAsync(
        AssignUserToSocietyRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("USER_NOT_FOUND", "User not found.", 404);
        _ = await _db.Societies.FirstOrDefaultAsync(x => x.Id == request.SocietyId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("SOCIETY_NOT_FOUND", "Society not found.", 404);
        var role = await _db.Roles.FirstOrDefaultAsync(x =>
            x.Id == request.RoleId && x.SocietyId == request.SocietyId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("ROLE_NOT_FOUND", "Role not found for this society.", 404);

        var membership = await _db.UserSocieties
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x =>
                x.UserId == request.UserId &&
                x.SocietyId == request.SocietyId &&
                !x.IsDeleted, cancellationToken);

        if (membership is null)
        {
            var hasOtherSociety = await _db.UserSocieties.AnyAsync(
                x => x.UserId == request.UserId && !x.IsDeleted && x.SocietyId != request.SocietyId,
                cancellationToken);
            if (hasOtherSociety)
                throw new AppException(
                    "USER_ALREADY_IN_SOCIETY",
                    "This user already belongs to another organization. One account = one society.",
                    403);

            membership = new UserSociety
            {
                UserId = request.UserId,
                SocietyId = request.SocietyId,
                RoleId = request.RoleId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.UserSocieties.Add(membership);
        }
        else
        {
            membership.RoleId = request.RoleId;
            membership.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            IsActive = user.IsActive,
            RoleId = role.Id,
            RoleName = role.Name,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<CurrentSubscriptionDto> ActivateSubscriptionAsync(
        ActivateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var society = await _db.Societies.FirstOrDefaultAsync(x => x.Id == request.SocietyId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("SOCIETY_NOT_FOUND", "Society not found.", 404);
        var plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == request.PlanId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("PLAN_NOT_FOUND", "Subscription plan not found.", 404);

        var activeRows = await _db.Subscriptions
            .Where(x => x.SocietyId == request.SocietyId && x.Status == SubscriptionStatuses.Active && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var active in activeRows)
        {
            active.Status = SubscriptionStatuses.Superseded;
            active.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var subscription = new Subscription
        {
            SocietyId = request.SocietyId,
            PlanId = request.PlanId,
            StartDate = startDate,
            EndDate = SubscriptionPeriodCalculator.ComputeEndDate(startDate, plan),
            Status = SubscriptionStatuses.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Subscriptions.Add(subscription);
        society.SubscriptionPlanId = plan.Id;
        society.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new CurrentSubscriptionDto
        {
            SubscriptionId = subscription.Id,
            SocietyId = request.SocietyId,
            PlanId = plan.Id,
            PlanName = plan.Name,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status
        };
    }
}

