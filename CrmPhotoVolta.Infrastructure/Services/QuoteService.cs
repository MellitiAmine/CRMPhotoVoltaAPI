using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Quotes;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class QuoteService : IQuoteService
{
    private readonly AppDbContext _app;

    public QuoteService(AppDbContext app)
    {
        _app = app;
    }

    public async Task<(IReadOnlyList<QuoteListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _app.Quotes.AsNoTracking().Where(x => x.SocietyId == societyId);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var s = pagination.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Title.ToLower().Contains(s) ||
                x.QuoteNumber.ToLower().Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);

        query = pagination.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.CreatedAt)
            : query.OrderByDescending(x => x.CreatedAt);

        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new QuoteListItemDto
            {
                Id = x.Id,
                QuoteNumber = x.QuoteNumber,
                Title = x.Title,
                Status = x.Status,
                TotalAmount = x.TotalAmount,
                Currency = x.Currency,
                LeadId = x.LeadId,
                ClientId = x.ClientId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return (items, pagination.ToMeta(total));
    }

    public async Task<QuoteDto> GetAsync(Guid societyId, Guid quoteId, CancellationToken cancellationToken = default)
    {
        var q = await _app.Quotes
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == quoteId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_NOT_FOUND", "Quote not found.", 404);

        return Map(q);
    }

    public async Task<QuoteDto> CreateAsync(Guid societyId, CreateQuoteRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException("VALIDATION_ERROR", "Title is required.", 400);

        await ValidateLinksAsync(societyId, request.LeadId, request.ClientId, request.DealId, cancellationToken);

        var number = await NextQuoteNumberAsync(societyId, cancellationToken);
        var quote = new Quote
        {
            SocietyId = societyId,
            LeadId = request.LeadId,
            ClientId = request.ClientId,
            DealId = request.DealId,
            QuoteNumber = number,
            Title = request.Title.Trim(),
            Status = "Draft",
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TND" : request.Currency.Trim().ToUpperInvariant(),
            ValidUntil = request.ValidUntil,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.Quotes.Add(quote);
        ReplaceItems(quote, societyId, request.Items);
        quote.TotalAmount = SumItems(quote.Items);
        await _app.SaveChangesAsync(cancellationToken);

        return Map(await ReloadQuoteAsync(quote.Id, cancellationToken));
    }

    public async Task<QuoteDto> UpdateAsync(Guid societyId, Guid quoteId, UpdateQuoteRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException("VALIDATION_ERROR", "Title is required.", 400);

        var quote = await _app.Quotes
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == quoteId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_NOT_FOUND", "Quote not found.", 404);

        if (quote.Status != "Draft")
            throw new AppException("QUOTE_LOCKED", "Only draft quotes can be edited.", 409);

        await ValidateLinksAsync(societyId, request.LeadId, request.ClientId, request.DealId, cancellationToken);

        quote.Title = request.Title.Trim();
        quote.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TND" : request.Currency.Trim().ToUpperInvariant();
        quote.ValidUntil = request.ValidUntil;
        quote.LeadId = request.LeadId;
        quote.ClientId = request.ClientId;
        quote.DealId = request.DealId;
        quote.UpdatedAt = DateTimeOffset.UtcNow;

        _app.QuoteItems.RemoveRange(quote.Items);
        ReplaceItems(quote, societyId, request.Items);
        quote.TotalAmount = SumItems(quote.Items);

        await _app.SaveChangesAsync(cancellationToken);

        return Map(await ReloadQuoteAsync(quoteId, cancellationToken));
    }

    public async Task DeleteAsync(Guid societyId, Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await _app.Quotes.FirstOrDefaultAsync(x => x.Id == quoteId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_NOT_FOUND", "Quote not found.", 404);

        if (quote.Status is "Accepted" or "Converted")
            throw new AppException("QUOTE_LOCKED", "Cannot delete an accepted or converted quote.", 409);

        quote.IsDeleted = true;
        quote.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }

    public async Task<QuoteDto> SendAsync(Guid societyId, Guid quoteId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var quote = await _app.Quotes.FirstOrDefaultAsync(x => x.Id == quoteId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_NOT_FOUND", "Quote not found.", 404);

        if (quote.Status != "Draft")
            throw new AppException("QUOTE_INVALID_STATE", "Only draft quotes can be sent.", 409);

        quote.Status = "Sent";
        quote.SentAt = DateTimeOffset.UtcNow;
        quote.UpdatedAt = DateTimeOffset.UtcNow;
        quote.UpdatedById = actorUserId;
        await _app.SaveChangesAsync(cancellationToken);

        return Map(await ReloadQuoteAsync(quoteId, cancellationToken));
    }

    public async Task<QuoteDto> AcceptAsync(Guid societyId, Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await _app.Quotes.FirstOrDefaultAsync(x => x.Id == quoteId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_NOT_FOUND", "Quote not found.", 404);

        if (quote.Status != "Sent")
            throw new AppException("QUOTE_INVALID_STATE", "Only sent quotes can be accepted.", 409);

        quote.Status = "Accepted";
        quote.AcceptedAt = DateTimeOffset.UtcNow;
        quote.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);

        return Map(await ReloadQuoteAsync(quoteId, cancellationToken));
    }

    public async Task<QuoteDto> RejectAsync(Guid societyId, Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await _app.Quotes.FirstOrDefaultAsync(x => x.Id == quoteId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_NOT_FOUND", "Quote not found.", 404);

        if (quote.Status != "Sent")
            throw new AppException("QUOTE_INVALID_STATE", "Only sent quotes can be rejected.", 409);

        quote.Status = "Rejected";
        quote.RejectedAt = DateTimeOffset.UtcNow;
        quote.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);

        return Map(await ReloadQuoteAsync(quoteId, cancellationToken));
    }

    public async Task<QuoteDto> ConvertToProjectAsync(
        Guid societyId,
        Guid quoteId,
        ConvertQuoteToProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectName))
            throw new AppException("VALIDATION_ERROR", "Project name is required.", 400);

        var quote = await _app.Quotes
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == quoteId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_NOT_FOUND", "Quote not found.", 404);

        if (quote.Status != "Accepted")
            throw new AppException("QUOTE_INVALID_STATE", "Only accepted quotes can be converted to a project.", 409);

        if (quote.ProjectId is not null)
            throw new AppException("QUOTE_ALREADY_CONVERTED", "This quote is already linked to a project.", 409);

        var clientId = quote.ClientId
            ?? throw new AppException("QUOTE_NO_CLIENT", "Quote must have a client before conversion.", 400);

        await EnsureClientAsync(societyId, clientId, cancellationToken);

        var project = new Project
        {
            SocietyId = societyId,
            ClientId = clientId,
            DealId = quote.DealId,
            Name = request.ProjectName.Trim(),
            Address = request.Address?.Trim(),
            Status = "Planned",
            ProgressPercent = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.Projects.Add(project);
        await _app.SaveChangesAsync(cancellationToken);

        quote.ProjectId = project.Id;
        quote.Status = "Converted";
        quote.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);

        return Map(await ReloadQuoteAsync(quoteId, cancellationToken));
    }

    private async Task<Quote> ReloadQuoteAsync(Guid id, CancellationToken cancellationToken) =>
        await _app.Quotes.AsNoTracking().Include(x => x.Items).FirstAsync(x => x.Id == id, cancellationToken);

    private static void ReplaceItems(Quote quote, Guid societyId, IReadOnlyList<QuoteItemWriteDto> items)
    {
        var order = 0;
        foreach (var line in items)
        {
            if (string.IsNullOrWhiteSpace(line.Description))
                continue;

            quote.Items.Add(new QuoteItem
            {
                SocietyId = societyId,
                QuoteId = quote.Id,
                Description = line.Description.Trim(),
                Quantity = line.Quantity <= 0 ? 1 : line.Quantity,
                UnitPrice = line.UnitPrice,
                SortOrder = line.SortOrder != 0 ? line.SortOrder : order,
                CreatedAt = DateTimeOffset.UtcNow
            });
            order++;
        }
    }

    private static decimal SumItems(IEnumerable<QuoteItem> items) =>
        items.Sum(x => x.Quantity * x.UnitPrice);

    private async Task<string> NextQuoteNumberAsync(Guid societyId, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"Q-{year}-";
        var count = await _app.Quotes.CountAsync(
            x => x.SocietyId == societyId && x.QuoteNumber.StartsWith(prefix),
            cancellationToken);
        return $"{prefix}{(count + 1):D4}";
    }

    private async Task ValidateLinksAsync(
        Guid societyId,
        Guid? leadId,
        Guid? clientId,
        Guid? dealId,
        CancellationToken cancellationToken)
    {
        if (leadId is { } l && !await _app.Leads.AnyAsync(x => x.Id == l && x.SocietyId == societyId, cancellationToken))
            throw new AppException("LEAD_NOT_FOUND", "Lead not found.", 404);

        if (clientId is { } c && !await _app.Clients.AnyAsync(x => x.Id == c && x.SocietyId == societyId, cancellationToken))
            throw new AppException("CLIENT_NOT_FOUND", "Client not found.", 404);

        if (dealId is { } d && !await _app.Deals.AnyAsync(x => x.Id == d && x.SocietyId == societyId, cancellationToken))
            throw new AppException("DEAL_NOT_FOUND", "Deal not found.", 404);
    }

    private async Task EnsureClientAsync(Guid societyId, Guid clientId, CancellationToken cancellationToken)
    {
        if (!await _app.Clients.AnyAsync(x => x.Id == clientId && x.SocietyId == societyId, cancellationToken))
            throw new AppException("CLIENT_NOT_FOUND", "Client not found.", 404);
    }

    private static QuoteDto Map(Quote q) => new()
    {
        Id = q.Id,
        QuoteNumber = q.QuoteNumber,
        Title = q.Title,
        Status = q.Status,
        Currency = q.Currency,
        TotalAmount = q.TotalAmount,
        ValidUntil = q.ValidUntil,
        LeadId = q.LeadId,
        ClientId = q.ClientId,
        DealId = q.DealId,
        ProjectId = q.ProjectId,
        SentAt = q.SentAt,
        AcceptedAt = q.AcceptedAt,
        RejectedAt = q.RejectedAt,
        CreatedAt = q.CreatedAt,
        Items = q.Items
            .OrderBy(x => x.SortOrder)
            .Select(x => new QuoteItemDto
            {
                Id = x.Id,
                Description = x.Description,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.Quantity * x.UnitPrice,
                SortOrder = x.SortOrder
            })
            .ToList()
    };
}
