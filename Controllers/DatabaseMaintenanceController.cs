using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Platform.DatabaseMaintenance;
using CrmPhotoVolta.Infrastructure.Seeding;
using CrmPhotoVolta.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CrmPhotoVoltaApis.Controllers;

/// <summary>One-off production bootstrap: migrations + seed. Guarded by <c>X-Maintenance-Key</c> and <c>DatabaseMaintenance:ApiKey</c>.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/system/database")]
public sealed class DatabaseMaintenanceController : ControllerBase
{
    private readonly IDatabaseMaintenanceService _maintenance;
    private readonly IOptions<DatabaseMaintenanceOptions> _options;

    public DatabaseMaintenanceController(
        IDatabaseMaintenanceService maintenance,
        IOptions<DatabaseMaintenanceOptions> options)
    {
        _maintenance = maintenance;
        _options = options;
    }

    /// <summary>Apply all pending EF migrations, then run catalog / platform / demo seed (same as startup when DB is up).</summary>
    [HttpPost("migrate-and-seed")]
    public async Task<IActionResult> MigrateAndSeed(
        [FromHeader(Name = "X-Maintenance-Key")] string? maintenanceKey,
        CancellationToken cancellationToken)
    {
        if (!EnsureKey(maintenanceKey, out var denied))
            return denied!;

        var result = await _maintenance.MigrateAndSeedAsync(cancellationToken);
        if (!result.Success)
            return StatusCode(500, ApiResponse.Fail("DATABASE_MAINTENANCE_FAILED", result.Error ?? "Unknown error", result));

        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Run seed pipeline only (no migrations). Use when schema is already migrated.</summary>
    [HttpPost("seed")]
    public async Task<IActionResult> SeedOnly(
        [FromHeader(Name = "X-Maintenance-Key")] string? maintenanceKey,
        CancellationToken cancellationToken)
    {
        if (!EnsureKey(maintenanceKey, out var denied))
            return denied!;

        var result = await _maintenance.SeedOnlyAsync(cancellationToken);
        if (!result.Success)
            return StatusCode(500, ApiResponse.Fail("DATABASE_MAINTENANCE_FAILED", result.Error ?? "Unknown error", result));

        return Ok(ApiResponse.Ok(result));
    }

    private bool EnsureKey(string? maintenanceKey, out IActionResult? denied)
    {
        denied = null;
        var expected = _options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(expected))
        {
            denied = StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse.Fail(
                    "MAINTENANCE_DISABLED",
                    "Set DatabaseMaintenance__ApiKey (or DatabaseMaintenance:ApiKey) to enable this endpoint."));
            return false;
        }

        if (!DatabaseMaintenanceService.ApiKeyMatches(expected, maintenanceKey))
        {
            denied = Unauthorized(ApiResponse.Fail("MAINTENANCE_UNAUTHORIZED", "Invalid or missing X-Maintenance-Key header."));
            return false;
        }

        return true;
    }
}
