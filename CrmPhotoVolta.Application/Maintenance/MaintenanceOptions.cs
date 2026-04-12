namespace CrmPhotoVolta.Application.Maintenance;

public sealed class MaintenanceOptions
{
    public const string SectionName = "Maintenance";

    /// <summary>
    /// Shared secret for <c>X-Maintenance-Secret</c> header. If empty, maintenance endpoints reject all requests.
    /// Set via <c>Maintenance__Secret</c> in production.
    /// </summary>
    public string Secret { get; set; } = string.Empty;
}
