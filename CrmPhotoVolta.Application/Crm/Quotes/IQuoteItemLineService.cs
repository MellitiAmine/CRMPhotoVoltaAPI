namespace CrmPhotoVolta.Application.Crm.Quotes;

public interface IQuoteItemLineService
{
    Task<QuoteDto> AddLineAsync(Guid societyId, CreateQuoteItemLineRequest request, CancellationToken cancellationToken = default);
    Task<QuoteDto> UpdateLineAsync(Guid societyId, Guid quoteItemId, UpdateQuoteItemLineRequest request, CancellationToken cancellationToken = default);
    Task<QuoteDto> DeleteLineAsync(Guid societyId, Guid quoteItemId, CancellationToken cancellationToken = default);
}
