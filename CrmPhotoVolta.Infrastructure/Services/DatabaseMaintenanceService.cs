using System.Security.Cryptography;
using System.Text;
using CrmPhotoVolta.Application.Platform.DatabaseMaintenance;
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
    private static readonly SemaphoreSlim Gate = new(1, 1);

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

    public Task<DatabaseMaintenanceResultDto> MigrateAndSeedAsync(CancellationToken cancellationToken = default) =>
        RunAsync(migrate: true, seed: true, cancellationToken);

    public Task<DatabaseMaintenanceResultDto> SeedOnlyAsync(CancellationToken cancellationToken = default) =>
        RunAsync(migrate: false, seed: true, cancellationToken);

    private async Task<DatabaseMaintenanceResultDto> RunAsync(bool migrate, bool seed, CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        var steps = new List<string>();
        try
        {
            if (migrate)
            {
                _logger.LogWarning("Database maintenance: applying EF migrations (Core, App, Platform).");
                await _core.Database.MigrateAsync(cancellationToken);
                steps.Add("Migrations: CoreDbContext applied.");
                await _app.Database.MigrateAsync(cancellationToken);
                steps.Add("Migrations: AppDbContext applied.");
                await _platform.Database.MigrateAsync(cancellationToken);
                steps.Add("Migrations: PlatformDbContext applied.");
            }

            if (seed)
            {
                if (!await _core.Database.CanConnectAsync(cancellationToken))
                    return new DatabaseMaintenanceResultDto { Success = false, Steps = steps, Error = "Cannot connect to PostgreSQL." };

                await DatabaseSeeder.EnsureSeedAsync(_core, cancellationToken);
                steps.Add("Seed: core catalog (permissions, plans).");

                await PlatformDatabaseSeeder.EnsureSeedAsync(_platform, cancellationToken);
                steps.Add("Seed: platform RBAC.");

                var platformSeed = _platformSeed.Value;
                if (platformSeed.Enabled)
                {
                    await PlatformDatabaseSeeder.EnsureSuperAdminUserAsync(
                        _platform,
                        platformSeed.PlatformAdminEmail,
                        platformSeed.PlatformAdminPassword,
                        cancellationToken);
                    steps.Add("Seed: platform super-admin user (PlatformSeed.Enabled=true).");
                }
                else
                    steps.Add("Seed: skipped platform super-admin (PlatformSeed.Enabled=false).");

                await PlatformDemoSeeder.RemoveLegacyTenantUserMatchingPlatformEmailAsync(
                    _core,
                    platformSeed.PlatformAdminEmail,
                    cancellationToken);
                steps.Add("Seed: legacy tenant cleanup for platform email.");

                await PlatformDemoSeeder.SeedAsync(_core, platformSeed, cancellationToken);
                steps.Add("Seed: demo societies / tenant bootstrap (per PlatformSeed).");
            }

            _logger.LogInformation("Database maintenance completed successfully.");
            return new DatabaseMaintenanceResultDto { Success = true, Steps = steps };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database maintenance failed.");
            return new DatabaseMaintenanceResultDto { Success = false, Steps = steps, Error = ex.Message };
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>Constant-time comparison for maintenance API key.</summary>
    public static bool ApiKeyMatches(string? configuredKey, string? providedKey)
    {
        if (string.IsNullOrEmpty(configuredKey) || providedKey is null)
            return false;

        var a = Encoding.UTF8.GetBytes(configuredKey);
        var b = Encoding.UTF8.GetBytes(providedKey);
        if (a.Length != b.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
