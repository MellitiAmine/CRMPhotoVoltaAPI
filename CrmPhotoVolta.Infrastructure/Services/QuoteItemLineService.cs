using CrmPhotoVolta.Application.Crm.Quotes;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class QuoteItemLineService : IQuoteItemLineService
{
    private readonly AppDbContext _app;
    private readonly IQuoteService _quotes;

    public QuoteItemLineService(AppDbContext app, IQuoteService quotes)
    {
        _app = app;
        _quotes = quotes;
    }

    public async Task<QuoteDto> AddLineAsync(Guid societyId, CreateQuoteItemLineRequest request, CancellationToken cancellationToken = default)
    {
        var quote = await _app.Quotes
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.QuoteId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_NOT_FOUND", "Quote not found.", 404);

        if (quote.Status != QuoteStatus.Draft)
            throw new AppException("QUOTE_LOCKED", "Only draft quotes can be edited.", 409);

        var discount = request.Discount;
        if (discount < 0 || discount > 100)
            throw new AppException("VALIDATION_ERROR", "Discount must be between 0 and 100.", 400);

        if (request.Quantity <= 0)
            throw new AppException("VALIDATION_ERROR", "Quantity must be greater than zero.", 400);

        var (desc, unitPrice, tvaRate, itemId) = await ResolveLinePricingAsync(
            societyId,
            request.ItemId,
            request.Description,
            request.UnitPrice,
            request.TvaRate,
            cancellationToken);

        var line = new QuoteItem
        {
            SocietyId = societyId,
            QuoteId = quote.Id,
            ItemId = itemId,
            Description = desc,
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            Discount = discount,
            TvaRate = tvaRate,
            SortOrder = request.SortOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };
        QuoteTotalsCalculator.RecomputeLine(line);
        quote.Items.Add(line);

        QuoteTotalsCalculator.ApplyQuoteAggregates(quote);
        quote.UpdatedAt = DateTimeOffset.UtcNow;

        await _app.SaveChangesAsync(cancellationToken);

        return await _quotes.GetAsync(societyId, quote.Id, cancellationToken);
    }

    public async Task<QuoteDto> UpdateLineAsync(
        Guid societyId,
        Guid quoteItemId,
        UpdateQuoteItemLineRequest request,
        CancellationToken cancellationToken = default)
    {
        var line = await _app.QuoteItems
            .Include(x => x.Quote)
            .ThenInclude(q => q!.Items)
            .FirstOrDefaultAsync(x => x.Id == quoteItemId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_ITEM_NOT_FOUND", "Quote line not found.", 404);

        var quote = line.Quote;
        if (quote.Status != QuoteStatus.Draft)
            throw new AppException("QUOTE_LOCKED", "Only draft quotes can be edited.", 409);

        if (request.Discount < 0 || request.Discount > 100)
            throw new AppException("VALIDATION_ERROR", "Discount must be between 0 and 100.", 400);

        if (request.Quantity <= 0)
            throw new AppException("VALIDATION_ERROR", "Quantity must be greater than zero.", 400);

        var (desc, unitPrice, tvaRate, itemId) = await ResolveLinePricingAsync(
            societyId,
            request.ItemId,
            request.Description,
            request.UnitPrice,
            request.TvaRate,
            cancellationToken);

        line.ItemId = itemId;
        line.Description = desc;
        line.Quantity = request.Quantity;
        line.UnitPrice = unitPrice;
        line.Discount = request.Discount;
        line.TvaRate = tvaRate;
        line.SortOrder = request.SortOrder;
        line.UpdatedAt = DateTimeOffset.UtcNow;

        QuoteTotalsCalculator.RecomputeLine(line);
        QuoteTotalsCalculator.ApplyQuoteAggregates(quote);
        quote.UpdatedAt = DateTimeOffset.UtcNow;

        await _app.SaveChangesAsync(cancellationToken);

        return await _quotes.GetAsync(societyId, quote.Id, cancellationToken);
    }

    public async Task<QuoteDto> DeleteLineAsync(Guid societyId, Guid quoteItemId, CancellationToken cancellationToken = default)
    {
        var line = await _app.QuoteItems
            .Include(x => x.Quote)
            .ThenInclude(q => q!.Items)
            .FirstOrDefaultAsync(x => x.Id == quoteItemId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("QUOTE_ITEM_NOT_FOUND", "Quote line not found.", 404);

        var quote = line.Quote;
        if (quote.Status != QuoteStatus.Draft)
            throw new AppException("QUOTE_LOCKED", "Only draft quotes can be edited.", 409);

        var quoteId = line.QuoteId;
        _app.QuoteItems.Remove(line);
        await _app.SaveChangesAsync(cancellationToken);

        var quoteReloaded = await _app.Quotes
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == quoteId && x.SocietyId == societyId, cancellationToken);

        QuoteTotalsCalculator.ApplyQuoteAggregates(quoteReloaded);
        quoteReloaded.UpdatedAt = DateTimeOffset.UtcNow;

        await _app.SaveChangesAsync(cancellationToken);

        return await _quotes.GetAsync(societyId, quoteId, cancellationToken);
    }

    private async Task<(string Description, decimal UnitPrice, decimal TvaRate, Guid? ItemId)> ResolveLinePricingAsync(
        Guid societyId,
        Guid? itemId,
        string? description,
        decimal unitPrice,
        decimal? tvaRate,
        CancellationToken cancellationToken)
    {
        if (itemId is { } cid)
        {
            var item = await _app.Items.FirstOrDefaultAsync(x => x.Id == cid && x.SocietyId == societyId, cancellationToken)
                ?? throw new AppException("ITEM_NOT_FOUND", "Item not found.", 404);

            var price = unitPrice > 0 ? unitPrice : item.DefaultPrice;
            var rate = tvaRate ?? item.TvaRate;
            var desc = string.IsNullOrWhiteSpace(description) ? item.Name : description.Trim();
            return (desc, price, rate, cid);
        }

        if (string.IsNullOrWhiteSpace(description))
            throw new AppException("VALIDATION_ERROR", "Description is required when ItemId is not set.", 400);

        var explicitTva = tvaRate ?? 0;
        return (description.Trim(), unitPrice, explicitTva, null);
    }
}
