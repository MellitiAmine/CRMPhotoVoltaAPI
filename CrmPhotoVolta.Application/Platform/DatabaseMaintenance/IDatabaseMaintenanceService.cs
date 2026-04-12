namespace CrmPhotoVolta.Application.Platform.DatabaseMaintenance;

public interface IDatabaseMaintenanceService
{
    /// <summary>Applies pending EF migrations (Core, App, Platform) then runs the same seed pipeline as startup.</summary>
    Task<DatabaseMaintenanceResultDto> MigrateAndSeedAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs seed only (no migrations). Use when schema is already up to date.</summary>
    Task<DatabaseMaintenanceResultDto> SeedOnlyAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseMaintenanceResultDto
{
    public bool Success { get; init; }
    public IReadOnlyList<string> Steps { get; init; } = Array.Empty<string>();
    public string? Error { get; init; }
}
