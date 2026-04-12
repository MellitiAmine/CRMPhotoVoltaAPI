using System.Text.Json;
using CrmPhotoVolta.Application.Crm.Settings;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class SocietySettingsService : ISocietySettingsService
{
    private readonly AppDbContext _app;

    public SocietySettingsService(AppDbContext app)
    {
        _app = app;
    }

    public async Task<SocietySettingsDto> GetAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var row = await _app.SocietySettings.FirstOrDefaultAsync(x => x.SocietyId == societyId, cancellationToken);
        if (row is null)
        {
            row = new SocietySettings
            {
                SocietyId = societyId,
                DataJson = "{}",
                CreatedAt = DateTimeOffset.UtcNow
            };
            _app.SocietySettings.Add(row);
            await _app.SaveChangesAsync(cancellationToken);
        }

        return new SocietySettingsDto { DataJson = row.DataJson };
    }

    public async Task<SocietySettingsDto> UpdateAsync(Guid societyId, UpdateSocietySettingsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            JsonDocument.Parse(request.DataJson);
        }
        catch
        {
            throw new AppException("INVALID_JSON", "DataJson must be valid JSON.", 400);
        }

        var row = await _app.SocietySettings.FirstOrDefaultAsync(x => x.SocietyId == societyId, cancellationToken);
        if (row is null)
        {
            row = new SocietySettings
            {
                SocietyId = societyId,
                DataJson = request.DataJson,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _app.SocietySettings.Add(row);
        }
        else
        {
            row.DataJson = request.DataJson;
            row.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _app.SaveChangesAsync(cancellationToken);
        return new SocietySettingsDto { DataJson = row.DataJson };
    }
}
