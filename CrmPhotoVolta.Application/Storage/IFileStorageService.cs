namespace CrmPhotoVolta.Application.Storage;

public sealed class FileUploadInput
{
    public required Guid SocietyId { get; init; }
    /// <summary>Relative folder, e.g. <c>installations/{id}/photos</c>.</summary>
    public required string RelativeFolder { get; init; }
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public required long Length { get; init; }
    public required Stream Content { get; init; }
    public bool ImagesOnly { get; init; }
}

public sealed class StoredFileResult
{
    /// <summary>Path served by static files, e.g. <c>/files/{society}/...</c>.</summary>
    public required string PublicPath { get; init; }
    /// <summary>Full URL when <see cref="FileStorageOptions.PublicBaseUrl"/> is set; otherwise same as <see cref="PublicPath"/>.</summary>
    public required string Url { get; init; }
    public required string StoredFileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
}

public interface IFileStorageService
{
    Task<StoredFileResult> SaveAsync(FileUploadInput input, CancellationToken cancellationToken = default);
    Task DeleteByPublicPathAsync(string publicPath, CancellationToken cancellationToken = default);
    string ToAbsoluteUrl(string publicPath);
}
