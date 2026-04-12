using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CrmPhotoVolta.Infrastructure.Data.Core;

public sealed class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
{
    public CoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CoreDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=crmPhotoVoltaDatabase;Username=postgres;Password=Admin",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "core"));
        return new CoreDbContext(optionsBuilder.Options);
    }
}
