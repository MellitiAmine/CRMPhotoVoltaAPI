using CrmPhotoVolta.Application.Crm.Dashboard;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _app;

    public DashboardService(AppDbContext app)
    {
        _app = app;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var kpis = await GetKpisAsync(societyId, cancellationToken);
        var pipeline = await GetPipelineAsync(societyId, cancellationToken);
        var revenue = await GetRevenueAsync(societyId, cancellationToken);
        var projects = await GetProjectsAsync(societyId, cancellationToken);

        return new DashboardOverviewDto
        {
            Kpis = kpis,
            Pipeline = pipeline,
            Revenue = revenue,
            Projects = projects
        };
    }

    public async Task<DashboardKpisDto> GetKpisAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var leadsCount = await _app.Leads.CountAsync(x => x.SocietyId == societyId, cancellationToken);
        var converted = await _app.Leads.CountAsync(
            x => x.SocietyId == societyId && (x.Status == LeadStatuses.Converted || x.Status == LeadStatuses.Won),
            cancellationToken);
        var conversionRate = leadsCount == 0 ? 0 : Math.Round(100m * converted / leadsCount, 2);

        var activeDeals = await _app.Deals.CountAsync(
            x => x.SocietyId == societyId && x.Stage != DealStages.Lost && x.Stage != DealStages.Won,
            cancellationToken);

        var acceptedQuotes = await _app.Quotes
            .Where(x => x.SocietyId == societyId && x.Status == QuoteStatus.Accepted)
            .SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0;

        var dealsValue = await _app.Deals
            .Where(x => x.SocietyId == societyId && x.Value != null)
            .SumAsync(x => x.Value ?? 0, cancellationToken);

        var revenueTotal = acceptedQuotes + dealsValue;

        var dealCountWithValue = await _app.Deals.CountAsync(
            x => x.SocietyId == societyId && x.Value != null,
            cancellationToken);
        var averageDealValue = dealCountWithValue == 0 ? 0 : Math.Round(dealsValue / dealCountWithValue, 2);

        var inst = await _app.Installations
            .Where(x => x.SocietyId == societyId)
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);

        var installationProgress = inst.Count == 0
            ? 0
            : Math.Round(100m * inst.Count(s => s == InstallationStatus.Completed) / inst.Count, 2);

        return new DashboardKpisDto
        {
            LeadsCount = leadsCount,
            ConversionRate = conversionRate,
            ActiveDeals = activeDeals,
            RevenueTotal = revenueTotal,
            AverageDealValue = averageDealValue,
            InstallationProgress = installationProgress
        };
    }

    public async Task<DashboardRevenueDto> GetRevenueAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var acceptedQuotesTotal = await _app.Quotes
            .Where(x => x.SocietyId == societyId && x.Status == QuoteStatus.Accepted)
            .SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0;

        var dealsValueTotal = await _app.Deals
            .Where(x => x.SocietyId == societyId && x.Value != null)
            .SumAsync(x => x.Value ?? 0, cancellationToken);

        var currency = await _app.Quotes
            .Where(x => x.SocietyId == societyId)
            .Select(x => x.Currency)
            .FirstOrDefaultAsync(cancellationToken) ?? "TND";

        return new DashboardRevenueDto
        {
            AcceptedQuotesTotal = acceptedQuotesTotal,
            DealsValueTotal = dealsValueTotal,
            Currency = currency
        };
    }

    public async Task<DashboardPipelineDto> GetPipelineAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var dealStages = await _app.Deals
            .Where(x => x.SocietyId == societyId)
            .GroupBy(x => x.Stage)
            .Select(g => new DashboardPipelineStageDto
            {
                Stage = g.Key,
                Count = g.Count(),
                ValueSum = g.Sum(x => x.Value ?? 0)
            })
            .ToListAsync(cancellationToken);

        var openLeads = await _app.Leads.CountAsync(
            x => x.SocietyId == societyId && x.Status != LeadStatuses.Lost && x.Status != LeadStatuses.Converted && x.Status != LeadStatuses.Won,
            cancellationToken);

        return new DashboardPipelineDto
        {
            DealStages = dealStages,
            OpenLeads = openLeads
        };
    }

    public async Task<DashboardProjectsDto> GetProjectsAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var rows = await _app.Projects
            .Where(x => x.SocietyId == societyId)
            .Select(x => new { x.Status, x.ProgressPercent })
            .ToListAsync(cancellationToken);

        var total = rows.Count;
        var active = rows.Count(x => x.Status != ProjectStatus.Done && x.Status != ProjectStatus.Cancelled);
        var completed = rows.Count(x => x.Status == ProjectStatus.Done);
        var avgProgress = total == 0 ? 0 : rows.Average(x => (double)x.ProgressPercent);

        return new DashboardProjectsDto
        {
            Total = total,
            Active = active,
            Completed = completed,
            AverageProgressPercent = Math.Round(avgProgress, 2)
        };
    }
}
