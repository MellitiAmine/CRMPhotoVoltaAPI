using System.Security.Cryptography;
using System.Text;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CrmPhotoVoltaApis.Controllers;

/// <summary>
/// Secured by <c>X-Maintenance-Secret</c> (see <see cref="MaintenanceOptions.Secret"/> / <c>Maintenance__Secret</c>).
/// Call only over HTTPS in production; rotate the secret after bootstrap.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/maintenance")]
public sealed class DatabaseMaintenanceController : ControllerBase
{
    private readonly IOptions<MaintenanceOptions> _options;
    private readonly IDatabaseMaintenanceService _maintenance;

    public DatabaseMaintenanceController(
        IOptions<MaintenanceOptions> options,
        IDatabaseMaintenanceService maintenance)
    {
        _options = options;
        _maintenance = maintenance;
    }

    /// <summary>Apply pending EF Core migrations for core, app, and platform schemas.</summary>
    [HttpPost("migrate")]
    public async Task<IActionResult> Migrate(
        [FromHeader(Name = "X-Maintenance-Secret")] string? secret,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized(secret))
            return MaintenanceRejected();

        var result = await _maintenance.MigrateAllAsync(cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Run catalog/platform/demo seeding (same pipeline as startup when DB is up).</summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(
        [FromHeader(Name = "X-Maintenance-Secret")] string? secret,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized(secret))
            return MaintenanceRejected();

        var result = await _maintenance.SeedAllAsync(cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Migrate then seed in one call.</summary>
    [HttpPost("migrate-and-seed")]
    public async Task<IActionResult> MigrateAndSeed(
        [FromHeader(Name = "X-Maintenance-Secret")] string? secret,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized(secret))
            return MaintenanceRejected();

        var result = await _maintenance.MigrateAndSeedAsync(cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult MaintenanceRejected()
    {
        var configured = !string.IsNullOrWhiteSpace(_options.Value.Secret);
        if (!configured)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse.Fail("MAINTENANCE_DISABLED", "Maintenance API is disabled (no Maintenance:Secret configured)."));
        }

        return Unauthorized(ApiResponse.Fail("MAINTENANCE_UNAUTHORIZED", "Invalid or missing X-Maintenance-Secret header."));
    }

    private bool IsAuthorized(string? providedSecret)
    {
        var expected = _options.Value.Secret ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expected))
            return false;
        if (string.IsNullOrWhiteSpace(providedSecret))
            return false;

        var ha = SHA256.HashData(Encoding.UTF8.GetBytes(providedSecret));
        var hb = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(ha, hb);
    }

    private IActionResult ToActionResult(DatabaseMaintenanceApplyResult result)
    {
        if (result.Success)
            return Ok(ApiResponse.Ok(result));

        return StatusCode(
            StatusCodes.Status500InternalServerError,
            ApiResponse.Fail("MAINTENANCE_FAILED", result.Error ?? "Maintenance operation failed.", result));
    }
}
