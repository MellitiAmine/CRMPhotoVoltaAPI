using System.Text.Json;
using System.Text.Json.Serialization;
using CrmPhotoVolta.Application.Crm.Leads;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class LeadJournalService : ILeadJournalService
{
    private readonly AppDbContext _app;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LeadJournalService(AppDbContext app)
    {
        _app = app;
    }

    public void Stage(
        Guid societyId,
        Guid leadId,
        Guid actorUserId,
        string action,
        string? relatedEntityType,
        Guid? relatedEntityId,
        object? metadata)
    {
        var now = DateTimeOffset.UtcNow;
        var json = metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOpts);
        _app.LeadJournalEntries.Add(new LeadJournalEntry
        {
            SocietyId = societyId,
            LeadId = leadId,
            Action = action,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            MetadataJson = json,
            CreatedAt = now,
            CreatedById = actorUserId
        });
    }

    public async Task<IReadOnlyList<LeadJournalEntryDto>> ListForLeadAsync(
        Guid societyId,
        Guid leadId,
        CancellationToken cancellationToken = default)
    {
        return await _app.LeadJournalEntries.AsNoTracking()
            .Where(x => x.LeadId == leadId && x.SocietyId == societyId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LeadJournalEntryDto
            {
                Id = x.Id,
                LeadId = x.LeadId,
                Action = x.Action,
                ActorUserId = x.CreatedById ?? Guid.Empty,
                CreatedAt = x.CreatedAt,
                RelatedEntityType = x.RelatedEntityType,
                RelatedEntityId = x.RelatedEntityId,
                MetadataJson = x.MetadataJson
            })
            .ToListAsync(cancellationToken);
    }
}
