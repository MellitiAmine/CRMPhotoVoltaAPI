namespace CrmPhotoVolta.Application.Crm.Dashboard;

public sealed class DashboardKpisDto
{
    public int LeadsCount { get; init; }
    public decimal ConversionRate { get; init; }
    public int ActiveDeals { get; init; }
    public decimal RevenueTotal { get; init; }
    public decimal AverageDealValue { get; init; }
    public decimal InstallationProgress { get; init; }
}

public sealed class DashboardPipelineStageDto
{
    public string Stage { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal? ValueSum { get; init; }
}

public sealed class DashboardPipelineDto
{
    public IReadOnlyList<DashboardPipelineStageDto> DealStages { get; init; } = Array.Empty<DashboardPipelineStageDto>();
    public int OpenLeads { get; init; }
}

public sealed class DashboardRevenueDto
{
    public decimal AcceptedQuotesTotal { get; init; }
    public decimal DealsValueTotal { get; init; }
    public string Currency { get; init; } = "TND";
}

public sealed class DashboardProjectsDto
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Completed { get; init; }
    public double AverageProgressPercent { get; init; }
}

public sealed class DashboardOverviewDto
{
    public DashboardKpisDto Kpis { get; init; } = new();
    public DashboardPipelineDto Pipeline { get; init; } = new();
    public DashboardRevenueDto Revenue { get; init; } = new();
    public DashboardProjectsDto Projects { get; init; } = new();
}
