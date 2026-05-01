using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Quotes;

/// <summary>HT / TVA / TTC aggregates for quotes (no I/O).</summary>
public static class QuoteTotalsCalculator
{
    public static decimal ComputeLineTotalHt(decimal quantity, decimal unitPrice, decimal discountPercent)
    {
        if (quantity < 0)
            throw new AppException("VALIDATION_ERROR", "Quantity cannot be negative.", 400);
        if (discountPercent < 0 || discountPercent > 100)
            throw new AppException("VALIDATION_ERROR", "Discount must be between 0 and 100.", 400);

        return quantity * unitPrice * (1 - discountPercent / 100m);
    }

    public static void ApplyQuoteAggregates(Quote quote)
    {
        var lines = quote.Items.Where(x => !x.IsDeleted).ToList();
        quote.TotalHt = lines.Sum(x => x.TotalHt);
        quote.TotalTva = lines.Sum(x => x.TotalHt * x.TvaRate / 100m);
        quote.TotalTtc = quote.TotalHt + quote.TotalTva;
        quote.TotalAmount = quote.TotalTtc;
    }

    public static void RecomputeLine(QuoteItem line)
    {
        line.TotalHt = ComputeLineTotalHt(line.Quantity, line.UnitPrice, line.Discount);
    }
}
