using CrmPhotoVolta.Application.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CrmPhotoVolta.Infrastructure.Storage;

public static class FileStorageServiceRegistration
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddSingleton<IFileStorageService>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<FileStorageOptions>>().Value;
            if (opts.Provider.Equals("Cloudinary", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Cloudinary storage is not implemented yet. Set FileStorage:Provider to Local.");
            return new LocalFileStorageService(
                sp.GetRequiredService<IWebHostEnvironment>(),
                sp.GetRequiredService<IOptions<FileStorageOptions>>());
        });
        return services;
    }
}
