namespace CrmPhotoVolta.Application.Maintenance;

public sealed class DatabaseMaintenanceApplyResult
{
    public bool Success { get; init; }
    public IReadOnlyList<string> Steps { get; init; } = Array.Empty<string>();
    public string? Error { get; init; }
}
