using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace CrmPhotoVolta.Infrastructure.Storage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _filesRoot;
    private readonly FileStorageOptions _options;

    public LocalFileStorageService(IWebHostEnvironment env, IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
        var webRoot = Path.Combine(env.ContentRootPath, _options.WebRootPath);
        _filesRoot = Path.Combine(webRoot, "files");
        Directory.CreateDirectory(_filesRoot);
    }

    public async Task<StoredFileResult> SaveAsync(FileUploadInput input, CancellationToken cancellationToken = default)
    {
        Validate(input);

        var ext = Path.GetExtension(input.OriginalFileName);
        var storedName = $"{Guid.NewGuid():N}{ext}";
        var societySegment = input.SocietyId.ToString("N");
        var relativeDir = Path.Combine(societySegment, input.RelativeFolder.Replace('/', Path.DirectorySeparatorChar));
        var absoluteDir = Path.Combine(_filesRoot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var absolutePath = Path.Combine(absoluteDir, storedName);
        await using (var fs = File.Create(absolutePath))
            await input.Content.CopyToAsync(fs, cancellationToken);

        var prefix = _options.PublicPathPrefix.TrimEnd('/');
        var publicPath = $"{prefix}/{societySegment}/{input.RelativeFolder.Trim('/')}/{storedName}".Replace('\\', '/');

        return new StoredFileResult
        {
            PublicPath    = publicPath,
            Url           = ToAbsoluteUrl(publicPath),
            StoredFileName = storedName,
            ContentType   = input.ContentType,
            SizeBytes     = input.Length
        };
    }

    public Task DeleteByPublicPathAsync(string publicPath, CancellationToken cancellationToken = default)
    {
        var physical = TryResolvePhysicalPath(publicPath);
        if (physical is not null && File.Exists(physical))
            File.Delete(physical);
        return Task.CompletedTask;
    }

    public string ToAbsoluteUrl(string publicPath)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return publicPath;
        return $"{_options.PublicBaseUrl.TrimEnd('/')}{publicPath}";
    }

    private void Validate(FileUploadInput input)
    {
        if (input.Length <= 0)
            throw new AppException("VALIDATION_ERROR", "File is empty.", 400);

        if (input.Length > _options.MaxFileSizeBytes)
            throw new AppException("VALIDATION_ERROR", $"File exceeds maximum size of {_options.MaxFileSizeBytes} bytes.", 400);

        var ext = Path.GetExtension(input.OriginalFileName).ToLowerInvariant();
        var allowed = input.ImagesOnly
            ? _options.AllowedImageExtensions
            : _options.AllowedDocumentExtensions;

        if (!allowed.Any(a => string.Equals(a, ext, StringComparison.OrdinalIgnoreCase)))
            throw new AppException("VALIDATION_ERROR", $"File type '{ext}' is not allowed.", 400);
    }

    private string? TryResolvePhysicalPath(string publicPath)
    {
        var prefix = _options.PublicPathPrefix.TrimEnd('/');
        if (!publicPath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            return null;

        var relative = publicPath[(prefix.Length + 1)..].Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_filesRoot, relative);
    }
}
