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
                TotalHt = x.TotalHt,
                TotalTva = x.TotalTva,
                TotalTtc = x.TotalTtc,
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

        var leadId = QuoteLinkResolver.Normalize(request.LeadId);
        var clientId = QuoteLinkResolver.Normalize(request.ClientId);
        var dealId = QuoteLinkResolver.Normalize(request.DealId);

        if (leadId is null && clientId is null && dealId is null)
            throw new AppException(
                "VALIDATION_ERROR",
                "At least one of leadId, clientId, or dealId is required.",
                400);

        await ValidateLinksAsync(societyId, leadId, clientId, dealId, cancellationToken);

        var number = await NextQuoteNumberAsync(societyId, cancellationToken);
        var quote = new Quote
        {
            SocietyId = societyId,
            LeadId = leadId,
            ClientId = clientId,
            DealId = dealId,
            QuoteNumber = number,
            Title = request.Title.Trim(),
            Status = QuoteStatus.Draft,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TND" : request.Currency.Trim().ToUpperInvariant(),
            ValidUntil = request.ValidUntil,
            QuoteDate = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.Quotes.Add(quote);
        await ReplaceItemsAsync(quote, societyId, request.Items ?? Array.Empty<QuoteItemWriteDto>(), cancellationToken);
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

        if (quote.Status != QuoteStatus.Draft)
            throw new AppException("QUOTE_LOCKED", "Only draft quotes can be edited.", 409);

        var leadId = QuoteLinkResolver.Normalize(request.LeadId);
        var clientId = QuoteLinkResolver.Normalize(request.ClientId);
        var dealId = QuoteLinkResolver.Normalize(request.DealId);

        if (leadId is null && clientId is null && dealId is null)
            throw new AppException(
                "VALIDATION_ERROR",
                "At least one of leadId, clientId, or dealId is required.",
                400);

        await ValidateLinksAsync(societyId, leadId, clientId, dealId, cancellationToken);

        quote.Title = request.Title.Trim();
        quote.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TND" : request.Currency.Trim().ToUpperInvariant();
        quote.ValidUntil = request.ValidUntil;
        quote.LeadId = leadId;
        quote.ClientId = clientId;
        quote.DealId = dealId;
        quote.UpdatedAt = DateTimeOffset.UtcNow;

        _app.QuoteItems.RemoveRange(quote.Items);
        quote.Items.Clear();
        await ReplaceItemsAsync(quote, societyId, request.Items ?? Array.Empty<QuoteItemWriteDto>(), cancellationToken);

        await _app.SaveChangesAsync(cancellationToken);

        return Map(await ReloadQuoteAsync(quoteId, cancellationToken));
    }

    public async Task DeleteAsync(Guid societyId, Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await _app.Quotes.FirstOrDefaultAsync(x => x.Id == quoteId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_NOT_FOUND", "Quote not found.", 404);

        if (quote.Status is QuoteStatus.Accepted or QuoteStatus.Converted)
            throw new AppException("QUOTE_LOCKED", "Cannot delete an accepted or converted quote.", 409);

        quote.IsDeleted = true;
        quote.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }

    public async Task<QuoteDto> SendAsync(Guid societyId, Guid quoteId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var quote = await _app.Quotes.FirstOrDefaultAsync(x => x.Id == quoteId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_NOT_FOUND", "Quote not found.", 404);

        if (quote.Status != QuoteStatus.Draft)
            throw new AppException("QUOTE_INVALID_STATE", "Only draft quotes can be sent.", 409);

        quote.Status = QuoteStatus.Sent;
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

        if (quote.Status != QuoteStatus.Sent)
            throw new AppException("QUOTE_INVALID_STATE", "Only sent quotes can be accepted.", 409);

        quote.Status = QuoteStatus.Accepted;
        quote.AcceptedAt = DateTimeOffset.UtcNow;
        quote.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);

        return Map(await ReloadQuoteAsync(quoteId, cancellationToken));
    }

    public async Task<QuoteDto> RejectAsync(Guid societyId, Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await _app.Quotes.FirstOrDefaultAsync(x => x.Id == quoteId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_NOT_FOUND", "Quote not found.", 404);

        if (quote.Status != QuoteStatus.Sent)
            throw new AppException("QUOTE_INVALID_STATE", "Only sent quotes can be rejected.", 409);

        quote.Status = QuoteStatus.Rejected;
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

        if (quote.Status != QuoteStatus.Accepted)
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
            Status = ProjectStatus.Planned,
            ProgressPercent = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.Projects.Add(project);
        await _app.SaveChangesAsync(cancellationToken);

        quote.ProjectId = project.Id;
        quote.Status = QuoteStatus.Converted;
        quote.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);

        return Map(await ReloadQuoteAsync(quoteId, cancellationToken));
    }

    private async Task<Quote> ReloadQuoteAsync(Guid id, CancellationToken cancellationToken) =>
        await _app.Quotes.AsNoTracking().Include(x => x.Items).FirstAsync(x => x.Id == id, cancellationToken);

    private async Task ReplaceItemsAsync(Quote quote, Guid societyId, IReadOnlyList<QuoteItemWriteDto> items, CancellationToken cancellationToken)
    {
        items ??= Array.Empty<QuoteItemWriteDto>();

        var itemIds = items
            .Select(x => QuoteLinkResolver.Normalize(x.ItemId))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var catalog = itemIds.Count == 0
            ? new Dictionary<Guid, Item>()
            : await _app.Items.AsNoTracking()
                .Where(x => itemIds.Contains(x.Id) && x.SocietyId == societyId)
                .ToDictionaryAsync(x => x.Id, cancellationToken);

        var order = 0;
        foreach (var line in items)
        {
            var lineItemId = QuoteLinkResolver.Normalize(line.ItemId);
            if (lineItemId is null && string.IsNullOrWhiteSpace(line.Description))
                continue;

            var discount = line.Discount ?? 0;
            if (discount < 0 || discount > 100)
                throw new AppException("VALIDATION_ERROR", "Discount must be between 0 and 100.", 400);

            var unitPrice = line.UnitPrice;
            decimal tvaRate;
            string desc;
            Guid? itemId = lineItemId;

            if (lineItemId is { } iid)
            {
                if (!catalog.TryGetValue(iid, out var cat))
                    throw new AppException("ITEM_NOT_FOUND", "Item not found.", 404);

                if (unitPrice <= 0)
                    unitPrice = cat.DefaultPrice;
                tvaRate = line.TvaRate ?? cat.TvaRate;
                desc = string.IsNullOrWhiteSpace(line.Description) ? cat.Name : line.Description.Trim();
            }
            else
            {
                tvaRate = line.TvaRate ?? 0;
                desc = line.Description.Trim();
            }

            var qty = line.Quantity <= 0 ? 1 : line.Quantity;
            var entity = new QuoteItem
            {
                SocietyId = societyId,
                QuoteId = quote.Id,
                ItemId = itemId,
                Description = desc,
                Quantity = qty,
                UnitPrice = unitPrice,
                Discount = discount,
                TvaRate = tvaRate,
                SortOrder = line.SortOrder != 0 ? line.SortOrder : order,
                CreatedAt = DateTimeOffset.UtcNow
            };
            QuoteTotalsCalculator.RecomputeLine(entity);
            quote.Items.Add(entity);
            order++;
        }

        QuoteTotalsCalculator.ApplyQuoteAggregates(quote);
    }

    private async Task<string> NextQuoteNumberAsync(Guid societyId, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"Q-{year}-";

        // Include soft-deleted rows: unique index (SocietyId, QuoteNumber) still reserves the number.
        var existing = await _app.Quotes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.SocietyId == societyId && x.QuoteNumber.StartsWith(prefix))
            .Select(x => x.QuoteNumber)
            .ToListAsync(cancellationToken);

        return SequentialReferenceGenerator.Next(prefix, existing);
    }

    private async Task ValidateLinksAsync(
        Guid societyId,
        Guid? leadId,
        Guid? clientId,
        Guid? dealId,
        CancellationToken cancellationToken)
    {
        leadId = QuoteLinkResolver.Normalize(leadId);
        clientId = QuoteLinkResolver.Normalize(clientId);
        dealId = QuoteLinkResolver.Normalize(dealId);

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
        TotalHt = q.TotalHt,
        TotalTva = q.TotalTva,
        TotalTtc = q.TotalTtc,
        QuoteDate = q.QuoteDate,
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
                ItemId = x.ItemId,
                Description = x.Description,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                Discount = x.Discount,
                TvaRate = x.TvaRate,
                TotalHt = x.TotalHt,
                LineTotal = x.TotalHt,
                SortOrder = x.SortOrder
            })
            .ToList()
    };
}
