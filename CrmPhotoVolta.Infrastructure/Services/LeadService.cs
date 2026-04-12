using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Leads;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class LeadService : ILeadService
{
    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;

    public LeadService(AppDbContext app, CoreDbContext core)
    {
        _app = app;
        _core = core;
    }

    public async Task<(IReadOnlyList<LeadListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _app.Leads.AsNoTracking().Where(x => x.SocietyId == societyId);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var s = pagination.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s) ||
                (x.Email != null && x.Email.ToLower().Contains(s)) ||
                (x.Phone != null && x.Phone.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);

        query = pagination.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? ApplySortAsc(query, pagination.SortBy)
            : ApplySortDesc(query, pagination.SortBy);

        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new LeadListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                Status = x.Status,
                AssignedToUserId = x.AssignedToUserId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return (items, pagination.ToMeta(total));
    }

    public async Task<LeadDto> GetAsync(Guid societyId, Guid leadId, CancellationToken cancellationToken = default)
    {
        var row = await _app.Leads.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        return Map(row);
    }

    public async Task<LeadDto> CreateAsync(Guid societyId, Guid actorUserId, CreateLeadRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        if (request.AssignedToUserId is { } assignee)
            await EnsureUserInSocietyAsync(societyId, assignee, cancellationToken);

        var lead = new Lead
        {
            SocietyId = societyId,
            Name = request.Name.Trim(),
            Phone = request.Phone?.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(),
            Address = request.Address?.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "New" : request.Status.Trim(),
            AssignedToUserId = request.AssignedToUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = actorUserId
        };

        _app.Leads.Add(lead);
        await _app.SaveChangesAsync(cancellationToken);

        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == lead.Id, cancellationToken));
    }

    public async Task<LeadDto> UpdateAsync(Guid societyId, Guid leadId, UpdateLeadRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        if (request.AssignedToUserId is { } assignee)
            await EnsureUserInSocietyAsync(societyId, assignee, cancellationToken);

        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        lead.Name = request.Name.Trim();
        lead.Phone = request.Phone?.Trim();
        lead.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        lead.Address = request.Address?.Trim();
        lead.Status = string.IsNullOrWhiteSpace(request.Status) ? "New" : request.Status.Trim();
        lead.AssignedToUserId = request.AssignedToUserId;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        await _app.SaveChangesAsync(cancellationToken);

        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken));
    }

    public async Task DeleteAsync(Guid societyId, Guid leadId, CancellationToken cancellationToken = default)
    {
        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        var hasDeals = await _app.Deals.AnyAsync(x => x.LeadId == leadId && x.SocietyId == societyId, cancellationToken);
        if (hasDeals)
            throw new AppException("LEAD_HAS_DEALS", "Cannot delete a lead that still has deals linked.", 409);

        lead.IsDeleted = true;
        lead.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeadActivityDto>> ListActivitiesAsync(Guid societyId, Guid leadId, CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, leadId, cancellationToken);

        return await _app.LeadActivities.AsNoTracking()
            .Where(x => x.LeadId == leadId && x.SocietyId == societyId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LeadActivityDto
            {
                Id = x.Id,
                LeadId = x.LeadId,
                Type = x.Type,
                Notes = x.Notes,
                CreatedByUserId = x.CreatedByUserId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<LeadActivityDto> AddActivityAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        AddLeadActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Type))
            throw new AppException("VALIDATION_ERROR", "Activity type is required.", 400);

        await EnsureLeadAsync(societyId, leadId, cancellationToken);

        var activity = new LeadActivity
        {
            SocietyId = societyId,
            LeadId = leadId,
            Type = request.Type.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedByUserId = actorUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = actorUserId
        };

        _app.LeadActivities.Add(activity);
        await _app.SaveChangesAsync(cancellationToken);

        var row = await _app.LeadActivities.AsNoTracking().FirstAsync(x => x.Id == activity.Id, cancellationToken);
        return new LeadActivityDto
        {
            Id = row.Id,
            LeadId = row.LeadId,
            Type = row.Type,
            Notes = row.Notes,
            CreatedByUserId = row.CreatedByUserId,
            CreatedAt = row.CreatedAt
        };
    }

    public async Task<LeadDto> AssignAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        AssignLeadRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureUserInSocietyAsync(societyId, request.UserId, cancellationToken);

        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        lead.AssignedToUserId = request.UserId;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        _app.LeadActivities.Add(new LeadActivity
        {
            SocietyId = societyId,
            LeadId = leadId,
            Type = "Assignment",
            Notes = $"Assigned to user {request.UserId}",
            CreatedByUserId = actorUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = actorUserId
        });

        await _app.SaveChangesAsync(cancellationToken);
        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken));
    }

    public async Task<ConvertLeadResultDto> ConvertAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        ConvertLeadRequest request,
        CancellationToken cancellationToken = default)
    {
        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        if (lead.Status == "Converted")
            throw new AppException("LEAD_ALREADY_CONVERTED", "Lead is already converted.", 409);

        var client = new Client
        {
            SocietyId = societyId,
            Name = lead.Name,
            Phone = lead.Phone,
            Email = lead.Email,
            Address = lead.Address,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.Clients.Add(client);
        await _app.SaveChangesAsync(cancellationToken);

        Guid? dealId = null;
        if (request.CreateDeal)
        {
            var title = string.IsNullOrWhiteSpace(request.DealTitle) ? $"Deal — {lead.Name}" : request.DealTitle!.Trim();
            var deal = new Deal
            {
                SocietyId = societyId,
                LeadId = leadId,
                Title = title,
                Stage = "New",
                CreatedAt = DateTimeOffset.UtcNow
            };
            _app.Deals.Add(deal);
            await _app.SaveChangesAsync(cancellationToken);
            dealId = deal.Id;
        }

        lead.Status = "Converted";
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        _app.LeadActivities.Add(new LeadActivity
        {
            SocietyId = societyId,
            LeadId = leadId,
            Type = "Converted",
            Notes = $"Client created: {client.Id}",
            CreatedByUserId = actorUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = actorUserId
        });

        await _app.SaveChangesAsync(cancellationToken);

        return new ConvertLeadResultDto
        {
            Lead = Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken)),
            ClientId = client.Id,
            DealId = dealId
        };
    }

    public async Task<LeadDto> MarkWonAsync(Guid societyId, Guid leadId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        lead.Status = "Won";
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        _app.LeadActivities.Add(new LeadActivity
        {
            SocietyId = societyId,
            LeadId = leadId,
            Type = "StatusChange",
            Notes = "Marked as Won",
            CreatedByUserId = actorUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = actorUserId
        });

        await _app.SaveChangesAsync(cancellationToken);
        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken));
    }

    public async Task<LeadDto> MarkLostAsync(Guid societyId, Guid leadId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var lead = await _app.Leads.FirstOrDefaultAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        lead.Status = "Lost";
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        _app.LeadActivities.Add(new LeadActivity
        {
            SocietyId = societyId,
            LeadId = leadId,
            Type = "StatusChange",
            Notes = "Marked as Lost",
            CreatedByUserId = actorUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = actorUserId
        });

        await _app.SaveChangesAsync(cancellationToken);
        return Map(await _app.Leads.AsNoTracking().FirstAsync(x => x.Id == leadId, cancellationToken));
    }

    public async Task<LeadActivityDto> AddNoteAsync(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        AddLeadNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            throw new AppException("VALIDATION_ERROR", "Note body is required.", 400);

        return await AddActivityAsync(societyId, leadId, actorUserId,
            new AddLeadActivityRequest { Type = "Note", Notes = request.Body.Trim() },
            cancellationToken);
    }

    public async Task<IReadOnlyList<LeadTimelineEntryDto>> GetTimelineAsync(
        Guid societyId,
        Guid leadId,
        CancellationToken cancellationToken = default)
    {
        await EnsureLeadAsync(societyId, leadId, cancellationToken);

        var activities = await _app.LeadActivities.AsNoTracking()
            .Where(x => x.LeadId == leadId && x.SocietyId == societyId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LeadTimelineEntryDto
            {
                Kind = "activity",
                At = x.CreatedAt,
                Title = x.Type,
                Detail = x.Notes,
                RefId = x.Id
            })
            .ToListAsync(cancellationToken);

        var quotes = await _app.Quotes.AsNoTracking()
            .Where(x => x.SocietyId == societyId && x.LeadId == leadId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LeadTimelineEntryDto
            {
                Kind = "quote",
                At = x.CreatedAt,
                Title = $"Quote {x.QuoteNumber}",
                Detail = $"{x.Status} — {x.TotalAmount} {x.Currency}",
                RefId = x.Id
            })
            .ToListAsync(cancellationToken);

        return activities.Concat(quotes).OrderByDescending(x => x.At).ToList();
    }

    private async Task EnsureLeadAsync(Guid societyId, Guid leadId, CancellationToken cancellationToken)
    {
        if (!await _app.Leads.AnyAsync(x => x.Id == leadId && x.SocietyId == societyId, cancellationToken))
            throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);
    }

    private async Task EnsureUserInSocietyAsync(Guid societyId, Guid userId, CancellationToken cancellationToken)
    {
        var ok = await _core.UserSocieties.AnyAsync(
            x => x.UserId == userId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken);

        if (!ok)
            throw new AppException("ASSIGNEE_NOT_IN_SOCIETY", "The selected user is not a member of this society.", 400);
    }

    private static LeadDto Map(Lead x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Phone = x.Phone,
        Email = x.Email,
        Address = x.Address,
        Status = x.Status,
        AssignedToUserId = x.AssignedToUserId,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };

    private static IQueryable<Lead> ApplySortAsc(IQueryable<Lead> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderBy(x => x.Name),
            "status" => query.OrderBy(x => x.Status),
            "email" => query.OrderBy(x => x.Email),
            _ => query.OrderBy(x => x.CreatedAt)
        };

    private static IQueryable<Lead> ApplySortDesc(IQueryable<Lead> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderByDescending(x => x.Name),
            "status" => query.OrderByDescending(x => x.Status),
            "email" => query.OrderByDescending(x => x.Email),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };
}
