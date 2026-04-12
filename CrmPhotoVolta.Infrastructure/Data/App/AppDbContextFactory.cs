using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CrmPhotoVolta.Infrastructure.Data.App;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=crmPhotoVoltaDatabase;Username=postgres;Password=Admin",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "app"));
        return new AppDbContext(optionsBuilder.Options);
    }
}
