namespace CrmPhotoVolta.Application.Storage;

/// <summary>
/// File storage configuration (appsettings section <c>FileStorage</c>).
/// Provider <c>Local</c> stores under wwwroot; <c>Cloudinary</c> reserved for future use.
/// </summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary><c>Local</c> (default) or <c>Cloudinary</c>.</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Folder under content root (default <c>wwwroot</c>).</summary>
    public string WebRootPath { get; set; } = "wwwroot";

    /// <summary>URL path prefix for static files (default <c>/files</c>).</summary>
    public string PublicPathPrefix { get; set; } = "/files";

    /// <summary>Optional API/public base URL prepended in responses (e.g. https://api.example.com).</summary>
    public string? PublicBaseUrl { get; set; }

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    public string[] AllowedImageExtensions { get; set; } =
        [".jpg", ".jpeg", ".png", ".webp", ".gif", ".heic"];

    public string[] AllowedDocumentExtensions { get; set; } =
        [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".webp"];

    public CloudinaryStorageOptions Cloudinary { get; set; } = new();
}

public sealed class CloudinaryStorageOptions
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string Folder { get; set; } = "crm-photovolta";
}
