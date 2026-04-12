using CrmPhotoVolta.Application.Crm.Pipeline;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class PipelineStageService : IPipelineStageService
{
    private readonly AppDbContext _app;

    public PipelineStageService(AppDbContext app)
    {
        _app = app;
    }

    public async Task<IReadOnlyList<PipelineStageDto>> ListAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        return await _app.PipelineStages.AsNoTracking()
            .Where(x => x.SocietyId == societyId)
            .OrderBy(x => x.Order)
            .Select(x => new PipelineStageDto { Id = x.Id, Name = x.Name, Order = x.Order })
            .ToListAsync(cancellationToken);
    }

    public async Task<PipelineStageDto> CreateAsync(Guid societyId, CreatePipelineStageRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        var row = new PipelineStage
        {
            SocietyId = societyId,
            Name = request.Name.Trim(),
            Order = request.Order,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.PipelineStages.Add(row);
        await _app.SaveChangesAsync(cancellationToken);

        return new PipelineStageDto { Id = row.Id, Name = row.Name, Order = row.Order };
    }

    public async Task<PipelineStageDto> UpdateAsync(Guid societyId, Guid id, UpdatePipelineStageRequest request, CancellationToken cancellationToken = default)
    {
        var row = await _app.PipelineStages.FirstOrDefaultAsync(x => x.Id == id && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PIPELINE_STAGE_NOT_FOUND", "Pipeline stage not found.", 404);

        row.Name = request.Name.Trim();
        row.Order = request.Order;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);

        return new PipelineStageDto { Id = row.Id, Name = row.Name, Order = row.Order };
    }

    public async Task DeleteAsync(Guid societyId, Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _app.PipelineStages.FirstOrDefaultAsync(x => x.Id == id && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PIPELINE_STAGE_NOT_FOUND", "Pipeline stage not found.", 404);

        row.IsDeleted = true;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }
}
