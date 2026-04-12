using CrmPhotoVolta.Application.Maintenance;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using CrmPhotoVolta.Infrastructure.Data.Platform;
using CrmPhotoVolta.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class DatabaseMaintenanceService : IDatabaseMaintenanceService
{
    private readonly CoreDbContext _core;
    private readonly AppDbContext _app;
    private readonly PlatformDbContext _platform;
    private readonly IOptions<PlatformSeedOptions> _platformSeed;
    private readonly ILogger<DatabaseMaintenanceService> _logger;

    public DatabaseMaintenanceService(
        CoreDbContext core,
        AppDbContext app,
        PlatformDbContext platform,
        IOptions<PlatformSeedOptions> platformSeed,
        ILogger<DatabaseMaintenanceService> logger)
    {
        _core = core;
        _app = app;
        _platform = platform;
        _platformSeed = platformSeed;
        _logger = logger;
    }

    public async Task RunStartupConnectivityAndSeedAsync(ILogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _core.Database.CanConnectAsync(cancellationToken))
            {
                logger.LogWarning(
                    "PostgreSQL is not reachable (CanConnectAsync=false). API will start; apply migrations and fix connectivity, then restart.");
                return;
            }

            logger.LogInformation("PostgreSQL connectivity check succeeded.");
            await RunSeedPipelineAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Database connectivity check or seeding failed. API will start; ensure DB is reachable and migrations are applied, then restart.");
        }
    }

    public async Task<DatabaseMaintenanceApplyResult> MigrateAllAsync(CancellationToken cancellationToken = default)
    {
        var steps = new List<string>();
        try
        {
            _logger.LogInformation("Maintenance: applying EF migrations (core, app, platform).");
            await _core.Database.MigrateAsync(cancellationToken);
            steps.Add("core: MigrateAsync completed");
            await _app.Database.MigrateAsync(cancellationToken);
            steps.Add("app: MigrateAsync completed");
            await _platform.Database.MigrateAsync(cancellationToken);
            steps.Add("platform: MigrateAsync completed");
            return new DatabaseMaintenanceApplyResult { Success = true, Steps = steps };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Maintenance: MigrateAll failed.");
            return new DatabaseMaintenanceApplyResult { Success = false, Steps = steps, Error = ex.Message };
        }
    }

    public async Task<DatabaseMaintenanceApplyResult> SeedAllAsync(CancellationToken cancellationToken = default)
    {
        var steps = new List<string>();
        try
        {
            await RunSeedPipelineAsync(cancellationToken);
            steps.Add("seed pipeline completed");
            return new DatabaseMaintenanceApplyResult { Success = true, Steps = steps };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Maintenance: SeedAll failed.");
            return new DatabaseMaintenanceApplyResult { Success = false, Steps = steps, Error = ex.Message };
        }
    }

    public async Task<DatabaseMaintenanceApplyResult> MigrateAndSeedAsync(CancellationToken cancellationToken = default)
    {
        var migrate = await MigrateAllAsync(cancellationToken);
        if (!migrate.Success)
        {
            return migrate;
        }

        var steps = migrate.Steps.ToList();
        var seed = await SeedAllAsync(cancellationToken);
        steps.AddRange(seed.Steps);
        return new DatabaseMaintenanceApplyResult
        {
            Success = seed.Success,
            Steps = steps,
            Error = seed.Error
        };
    }

    private async Task RunSeedPipelineAsync(CancellationToken cancellationToken)
    {
        await DatabaseSeeder.EnsureSeedAsync(_core, cancellationToken);
        await PlatformDatabaseSeeder.EnsureSeedAsync(_platform, cancellationToken);

        var platformSeed = _platformSeed.Value;
        if (platformSeed.Enabled)
        {
            await PlatformDatabaseSeeder.EnsureSuperAdminUserAsync(
                _platform,
                platformSeed.PlatformAdminEmail,
                platformSeed.PlatformAdminPassword,
                cancellationToken);
        }

        await PlatformDemoSeeder.RemoveLegacyTenantUserMatchingPlatformEmailAsync(
            _core,
            platformSeed.PlatformAdminEmail,
            cancellationToken);

        await PlatformDemoSeeder.SeedAsync(_core, platformSeed, cancellationToken);
    }
}
