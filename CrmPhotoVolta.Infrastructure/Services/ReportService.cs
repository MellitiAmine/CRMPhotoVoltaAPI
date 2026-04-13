using CrmPhotoVolta.Application.Crm.Reports;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class ReportService : IReportService
{
    private readonly AppDbContext _app;

    public ReportService(AppDbContext app)
    {
        _app = app;
    }

    public async Task<SalesReportDto> GetSalesAsync(Guid societyId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var qAccepted = _app.Quotes.Where(x => x.SocietyId == societyId && x.Status == QuoteStatus.Accepted);
        if (from is { } f)
            qAccepted = qAccepted.Where(x => x.AcceptedAt != null && DateOnly.FromDateTime(x.AcceptedAt.Value.UtcDateTime) >= f);
        if (to is { } t)
            qAccepted = qAccepted.Where(x => x.AcceptedAt != null && DateOnly.FromDateTime(x.AcceptedAt.Value.UtcDateTime) <= t);

        var quotesAccepted = await qAccepted.CountAsync(cancellationToken);
        var quotesRevenue = await qAccepted.SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0;

        var dealsClosed = await _app.Deals.CountAsync(
            x => x.SocietyId == societyId && x.Stage == DealStages.Won,
            cancellationToken);

        var dealsValue = await _app.Deals
            .Where(x => x.SocietyId == societyId && x.Stage == DealStages.Won && x.Value != null)
            .SumAsync(x => x.Value ?? 0, cancellationToken);

        return new SalesReportDto
        {
            QuotesAccepted = quotesAccepted,
            QuotesRevenue = quotesRevenue,
            DealsClosed = dealsClosed,
            DealsValue = dealsValue
        };
    }

    public async Task<ProjectsReportDto> GetProjectsAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var rows = await _app.Projects
            .Where(x => x.SocietyId == societyId)
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new ProjectsReportDto
        {
            Total = rows.Sum(x => x.Count),
            ByStatus = rows.ToDictionary(x => x.Status.ToString(), x => x.Count)
        };
    }

    public async Task<TechniciansReportDto> GetTechniciansAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var groups = await _app.Installations
            .Where(x => x.SocietyId == societyId)
            .GroupBy(x => x.TechnicianId)
            .Select(g => new
            {
                UserId = g.Key,
                Open = g.Count(x => x.Status != InstallationStatus.Completed && x.Status != InstallationStatus.Cancelled),
                Done = g.Count(x => x.Status == InstallationStatus.Completed)
            })
            .ToListAsync(cancellationToken);

        var list = groups
            .Select(x => new TechnicianLoadDto
            {
                UserId = x.UserId,
                OpenInstallations = x.Open,
                CompletedInstallations = x.Done
            })
            .ToList();

        return new TechniciansReportDto { Technicians = list };
    }

    public async Task<ConversionReportDto> GetConversionAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        var total = await _app.Leads.CountAsync(x => x.SocietyId == societyId, cancellationToken);
        var converted = await _app.Leads.CountAsync(
            x => x.SocietyId == societyId && (x.Status == LeadStatuses.Converted || x.Status == LeadStatuses.Won),
            cancellationToken);

        var pct = total == 0 ? 0 : Math.Round(100m * converted / total, 2);

        return new ConversionReportDto
        {
            LeadsTotal = total,
            LeadsConverted = converted,
            ConversionPercent = pct
        };
    }
}
