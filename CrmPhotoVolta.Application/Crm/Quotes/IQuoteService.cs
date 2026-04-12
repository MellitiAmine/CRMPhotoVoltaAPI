using CrmPhotoVolta.Application.Common;

namespace CrmPhotoVolta.Application.Crm.Quotes;

public interface IQuoteService
{
    Task<(IReadOnlyList<QuoteListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    Task<QuoteDto> GetAsync(Guid societyId, Guid quoteId, CancellationToken cancellationToken = default);
    Task<QuoteDto> CreateAsync(Guid societyId, CreateQuoteRequest request, CancellationToken cancellationToken = default);
    Task<QuoteDto> UpdateAsync(Guid societyId, Guid quoteId, UpdateQuoteRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid societyId, Guid quoteId, CancellationToken cancellationToken = default);

    Task<QuoteDto> SendAsync(Guid societyId, Guid quoteId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<QuoteDto> AcceptAsync(Guid societyId, Guid quoteId, CancellationToken cancellationToken = default);
    Task<QuoteDto> RejectAsync(Guid societyId, Guid quoteId, CancellationToken cancellationToken = default);
    Task<QuoteDto> ConvertToProjectAsync(Guid societyId, Guid quoteId, ConvertQuoteToProjectRequest request, CancellationToken cancellationToken = default);
}
