using CrmPhotoVolta.Application.Crm.Items;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class ItemService : IItemService
{
    private readonly AppDbContext _app;

    public ItemService(AppDbContext app)
    {
        _app = app;
    }

    public async Task<ItemDto> CreateAsync(Guid societyId, CreateItemRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        var unit = string.IsNullOrWhiteSpace(request.Unit) ? "piece" : request.Unit.Trim();
        if (request.TvaRate < 0 || request.TvaRate > 100)
            throw new AppException("VALIDATION_ERROR", "TvaRate must be between 0 and 100.", 400);

        var row = new Item
        {
            SocietyId = societyId,
            Name = request.Name.Trim(),
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            Unit = unit,
            DefaultPrice = request.DefaultPrice,
            TvaRate = request.TvaRate,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.Items.Add(row);
        await _app.SaveChangesAsync(cancellationToken);

        return Map(row);
    }

    public async Task<IReadOnlyList<ItemDto>> ListAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var list = await _app.Items.AsNoTracking()
            .Where(x => x.SocietyId == societyId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return list.ConvertAll(Map);
    }

    private static ItemDto Map(Item x) => new()
    {
        Id = x.Id,
        SocietyId = x.SocietyId,
        Name = x.Name,
        Reference = x.Reference,
        Unit = x.Unit,
        DefaultPrice = x.DefaultPrice,
        TvaRate = x.TvaRate,
        CreatedAt = x.CreatedAt
    };
}
