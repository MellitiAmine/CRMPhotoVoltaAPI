using Microsoft.Extensions.Logging;

namespace CrmPhotoVolta.Application.Maintenance;

public interface IDatabaseMaintenanceService
{
    /// <summary>Startup path: CanConnect then seed only (no migrations).</summary>
    Task RunStartupConnectivityAndSeedAsync(ILogger logger, CancellationToken cancellationToken = default);

    Task<DatabaseMaintenanceApplyResult> MigrateAllAsync(CancellationToken cancellationToken = default);

    Task<DatabaseMaintenanceApplyResult> SeedAllAsync(CancellationToken cancellationToken = default);

    Task<DatabaseMaintenanceApplyResult> MigrateAndSeedAsync(CancellationToken cancellationToken = default);
}
