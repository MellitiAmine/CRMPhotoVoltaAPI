namespace CrmPhotoVolta.Application.Crm.Reports;

public interface IReportService
{
    Task<SalesReportDto> GetSalesAsync(Guid societyId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
    Task<ProjectsReportDto> GetProjectsAsync(Guid societyId, CancellationToken cancellationToken = default);
    Task<TechniciansReportDto> GetTechniciansAsync(Guid societyId, CancellationToken cancellationToken = default);
    Task<ConversionReportDto> GetConversionAsync(Guid societyId, CancellationToken cancellationToken = default);
}

public sealed class SalesReportDto
{
    public int QuotesAccepted { get; init; }
    public decimal QuotesRevenue { get; init; }
    public int DealsClosed { get; init; }
    public decimal DealsValue { get; init; }
}

public sealed class ProjectsReportDto
{
    public int Total { get; init; }
    public IReadOnlyDictionary<string, int> ByStatus { get; init; } = new Dictionary<string, int>();
}

public sealed class TechniciansReportDto
{
    public IReadOnlyList<TechnicianLoadDto> Technicians { get; init; } = Array.Empty<TechnicianLoadDto>();
}

public sealed class TechnicianLoadDto
{
    public Guid UserId { get; init; }
    public int OpenInstallations { get; init; }
    public int CompletedInstallations { get; init; }
}

public sealed class ConversionReportDto
{
    public int LeadsTotal { get; init; }
    public int LeadsConverted { get; init; }
    public decimal ConversionPercent { get; init; }
}
