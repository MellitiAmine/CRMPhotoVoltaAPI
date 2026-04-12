namespace CrmPhotoVolta.Application.Crm.Dashboard;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(Guid societyId, CancellationToken cancellationToken = default);
    Task<DashboardKpisDto> GetKpisAsync(Guid societyId, CancellationToken cancellationToken = default);
    Task<DashboardRevenueDto> GetRevenueAsync(Guid societyId, CancellationToken cancellationToken = default);
    Task<DashboardPipelineDto> GetPipelineAsync(Guid societyId, CancellationToken cancellationToken = default);
    Task<DashboardProjectsDto> GetProjectsAsync(Guid societyId, CancellationToken cancellationToken = default);
}
