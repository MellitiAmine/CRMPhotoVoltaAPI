using CrmPhotoVolta.Application.Crm.Commercials;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class CommercialKpiSyncService : ICommercialKpiSyncService
{
    private readonly AppDbContext _db;

    public CommercialKpiSyncService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SyncForUserAsync(Guid societyId, Guid userId, CancellationToken ct = default)
    {
        var profile = await _db.CommercialProfiles
            .FirstOrDefaultAsync(c => c.SocietyId == societyId && c.UserId == userId, ct);
        if (profile is null)
            return;

        await ApplyKpisAsync(profile, societyId, userId, ct);
    }

    public async Task SyncForProfileAsync(Guid societyId, Guid profileId, CancellationToken ct = default)
    {
        var profile = await _db.CommercialProfiles
            .FirstOrDefaultAsync(c => c.SocietyId == societyId && c.Id == profileId, ct);
        if (profile is null)
            return;

        await ApplyKpisAsync(profile, societyId, profile.UserId, ct);
    }

    private async Task ApplyKpisAsync(
        CommercialProfile profile,
        Guid societyId,
        Guid userId,
        CancellationToken ct)
    {
        var (monthStart, monthEnd) = CurrentMonthUtcRange();

        var leadsQuery = _db.Leads.AsNoTracking()
            .Where(l => l.SocietyId == societyId && l.AssignedToUserId == userId);

        var leadsAssigned = await leadsQuery.CountAsync(ct);
        var dealsWon = await leadsQuery.CountAsync(l => l.Status == LeadStatuses.Gagne, ct);
        var conversionRate = leadsAssigned > 0
            ? Math.Round(dealsWon * 100.0 / leadsAssigned, 1)
            : 0;

        var leadIds = await leadsQuery.Select(l => l.Id).ToListAsync(ct);

        var activitiesCreated = leadIds.Count == 0
            ? 0
            : await _db.LeadActivities.AsNoTracking()
                .Where(a => leadIds.Contains(a.LeadId)
                    && a.CreatedAt >= monthStart
                    && a.CreatedAt < monthEnd
                    && a.Type != LeadActivityType.Assignment
                    && a.Type != LeadActivityType.StatusChange
                    && a.Type != LeadActivityType.Converted)
                .CountAsync(ct);

        var events = await _db.Events.AsNoTracking()
            .Where(e => e.SocietyId == societyId
                && e.StartDate >= monthStart
                && e.StartDate < monthEnd)
            .Select(e => new { e.AssignedToUserId, e.Participants })
            .ToListAsync(ct);

        var meetingsParticipated = events.Count(e =>
            e.AssignedToUserId == userId || e.Participants.Contains(userId));

        var quotesGenerated = leadIds.Count == 0
            ? 0
            : await _db.Quotes.AsNoTracking()
                .Where(q => q.SocietyId == societyId
                    && q.LeadId.HasValue
                    && leadIds.Contains(q.LeadId.Value)
                    && q.CreatedAt >= monthStart
                    && q.CreatedAt < monthEnd)
                .CountAsync(ct);

        var wonRevenue = await leadsQuery
            .Where(l => l.Status == LeadStatuses.Gagne)
            .Select(l => l.MontantEstimé ?? 0)
            .ToListAsync(ct);

        var quoteRevenue = leadIds.Count == 0
            ? 0m
            : await _db.Quotes.AsNoTracking()
                .Where(q => q.SocietyId == societyId
                    && q.LeadId.HasValue
                    && leadIds.Contains(q.LeadId.Value)
                    && (q.Status == QuoteStatus.Accepted || q.Status == QuoteStatus.Converted)
                    && q.CreatedAt >= monthStart
                    && q.CreatedAt < monthEnd)
                .SumAsync(q => (decimal?)q.TotalTtc, ct) ?? 0m;

        var revenueFromLeads = wonRevenue.Sum(v => (decimal)v);
        var revenueGenerated = revenueFromLeads > 0 ? revenueFromLeads : quoteRevenue;

        profile.KpiActivitiesCreated = activitiesCreated;
        profile.KpiMeetingsParticipated = meetingsParticipated;
        profile.KpiLeadsAssigned = leadsAssigned;
        profile.KpiDealsWon = dealsWon;
        profile.KpiQuotesGenerated = quotesGenerated;
        profile.KpiRevenueGenerated = revenueGenerated;
        profile.KpiConversionRate = conversionRate;

        CommercialProfileScoring.ComputeAndApplyScore(profile);
        profile.ScoredAt = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private static (DateTimeOffset Start, DateTimeOffset End) CurrentMonthUtcRange()
    {
        var now = DateTimeOffset.UtcNow;
        var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return (start, start.AddMonths(1));
    }
}
