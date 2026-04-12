namespace CrmPhotoVolta.Infrastructure.Seeding;

/// <summary>Guards <see cref="Services.DatabaseMaintenanceService"/> HTTP endpoints. Set <see cref="ApiKey"/> via env <c>DatabaseMaintenance__ApiKey</c>.</summary>
public sealed class DatabaseMaintenanceOptions
{
    public const string SectionName = "DatabaseMaintenance";

    /// <summary>Shared secret for <c>X-Maintenance-Key</c> header. If empty, maintenance endpoints reject all calls.</summary>
    public string ApiKey { get; set; } = "";
}
