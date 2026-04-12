using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CrmPhotoVolta.Infrastructure.Data.Platform;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=crmPhotoVoltaDatabase;Username=postgres;Password=Admin",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "platform"));
        return new PlatformDbContext(optionsBuilder.Options);
    }
}
