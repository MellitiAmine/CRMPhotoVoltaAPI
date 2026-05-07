using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Deals;
using CrmPhotoVolta.Application.Crm.Leads;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class DealService : IDealService
{
    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;

    public DealService(AppDbContext app, CoreDbContext core)
    {
        _app = app;
        _core = core;
    }

    public async Task<(IReadOnlyList<DealListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _app.Deals.AsNoTracking().Where(x => x.SocietyId == societyId);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var s = pagination.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Title.ToLower().Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);

        query = pagination.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? ApplySortAsc(query, pagination.SortBy)
            : ApplySortDesc(query, pagination.SortBy);

        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new DealListItemDto
            {
                Id = x.Id,
                LeadId = x.LeadId,
                Title = x.Title,
                Value = x.Value,
                Stage = x.Stage,
                AssignedToUserId = x.AssignedToUserId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return (items, pagination.ToMeta(total));
    }

    public async Task<DealDto> GetAsync(Guid societyId, Guid dealId, CancellationToken cancellationToken = default)
    {
        var row = await _app.Deals
            .AsNoTracking()
            .Include(x => x.Lead)
            .FirstOrDefaultAsync(x => x.Id == dealId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("DEAL_NOT_FOUND", "Deal not found.", 404);

        return Map(row);
    }

    public async Task<DealDto> CreateAsync(Guid societyId, CreateDealRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException("VALIDATION_ERROR", "Title is required.", 400);

        if (request.LeadId is { } lid)
            await EnsureLeadInSocietyAsync(societyId, lid, cancellationToken);

        if (request.AssignedToUserId is { } assignee)
            await EnsureUserInSocietyAsync(societyId, assignee, cancellationToken);

        var deal = new Deal
        {
            SocietyId = societyId,
            LeadId = request.LeadId,
            Title = request.Title.Trim(),
            Value = request.Value,
            Stage = string.IsNullOrWhiteSpace(request.Stage) ? DealStages.New : request.Stage.Trim(),
            AssignedToUserId = request.AssignedToUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.Deals.Add(deal);
        await _app.SaveChangesAsync(cancellationToken);

        return Map(await LoadDealWithLeadAsync(societyId, deal.Id, cancellationToken));
    }

    public async Task<DealDto> UpdateAsync(Guid societyId, Guid dealId, UpdateDealRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException("VALIDATION_ERROR", "Title is required.", 400);

        if (request.LeadId is { } lid)
            await EnsureLeadInSocietyAsync(societyId, lid, cancellationToken);

        if (request.AssignedToUserId is { } assignee)
            await EnsureUserInSocietyAsync(societyId, assignee, cancellationToken);

        var deal = await _app.Deals.FirstOrDefaultAsync(x => x.Id == dealId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("DEAL_NOT_FOUND", "Deal not found.", 404);

        deal.LeadId = request.LeadId;
        deal.Title = request.Title.Trim();
        deal.Value = request.Value;
        deal.Stage = string.IsNullOrWhiteSpace(request.Stage) ? DealStages.New : request.Stage.Trim();
        deal.AssignedToUserId = request.AssignedToUserId;
        deal.UpdatedAt = DateTimeOffset.UtcNow;

        await _app.SaveChangesAsync(cancellationToken);

        return Map(await LoadDealWithLeadAsync(societyId, dealId, cancellationToken));
    }

    public async Task DeleteAsync(Guid societyId, Guid dealId, CancellationToken cancellationToken = default)
    {
        var deal = await _app.Deals.FirstOrDefaultAsync(x => x.Id == dealId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("DEAL_NOT_FOUND", "Deal not found.", 404);

        var hasProjects = await _app.Projects.AnyAsync(x => x.DealId == dealId && x.SocietyId == societyId, cancellationToken);
        if (hasProjects)
            throw new AppException("DEAL_HAS_PROJECTS", "Cannot delete a deal that still has projects linked.", 409);

        deal.IsDeleted = true;
        deal.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureLeadInSocietyAsync(Guid societyId, Guid leadId, CancellationToken cancellationToken)
    {
        if (!await _app.Leads.AnyAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken))
            throw new AppException("LEAD_NOT_FOUND", "Lead not found in this society.", 404);
    }

    private async Task EnsureUserInSocietyAsync(Guid societyId, Guid userId, CancellationToken cancellationToken)
    {
        var ok = await _core.UserSocieties.AnyAsync(
            x => x.UserId == userId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken);

        if (!ok)
            throw new AppException("ASSIGNEE_NOT_IN_SOCIETY", "The selected user is not a member of this society.", 400);
    }

    private static DealDto Map(Deal x) => new()
    {
        Id = x.Id,
        LeadId = x.LeadId,
        Title = x.Title,
        Value = x.Value,
        Stage = x.Stage,
        AssignedToUserId = x.AssignedToUserId,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt,
        LeadInfo = x.Lead is null
            ? null
            : new DealLeadInfoDto
            {
                Id = x.Lead.Id,
                Name = x.Lead.Name,
                Status = x.Lead.Status,
                Lvi = x.Lead.Lvi,
                Sd = x.Lead.Sd,
                ScoredAt = x.Lead.ScoredAt,
                Temperature = x.Lead.Temperature,
                Priority = x.Lead.Priority,
                ScoreBreakdown = x.Lead.ScoreBreakdownInteraction is null
                    ? null
                    : new LeadScoreBreakdownDto
                    {
                        Interaction = x.Lead.ScoreBreakdownInteraction ?? 0,
                        Intention = x.Lead.ScoreBreakdownIntention ?? 0,
                        Satisfaction = x.Lead.ScoreBreakdownSatisfaction ?? 0,
                        Activity = x.Lead.ScoreBreakdownActivity ?? 0,
                        Potential = x.Lead.ScoreBreakdownPotential ?? 0,
                        Penalties = x.Lead.ScoreBreakdownPenalties ?? 0
                    }
            }
    };

    private async Task<Deal> LoadDealWithLeadAsync(Guid societyId, Guid dealId, CancellationToken cancellationToken)
    {
        return await _app.Deals
            .AsNoTracking()
            .Include(x => x.Lead)
            .FirstAsync(x => x.Id == dealId && x.SocietyId == societyId, cancellationToken);
    }

    private static IQueryable<Deal> ApplySortAsc(IQueryable<Deal> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "title" => query.OrderBy(x => x.Title),
            "stage" => query.OrderBy(x => x.Stage),
            "value" => query.OrderBy(x => x.Value),
            _ => query.OrderBy(x => x.CreatedAt)
        };

    private static IQueryable<Deal> ApplySortDesc(IQueryable<Deal> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "title" => query.OrderByDescending(x => x.Title),
            "stage" => query.OrderByDescending(x => x.Stage),
            "value" => query.OrderByDescending(x => x.Value),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };
}
