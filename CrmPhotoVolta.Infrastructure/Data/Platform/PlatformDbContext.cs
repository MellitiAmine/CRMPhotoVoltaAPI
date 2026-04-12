using CrmPhotoVolta.Domain.Platform;
using CrmPhotoVolta.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Data.Platform;

public sealed class PlatformDbContext : DbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
    {
    }

    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<PlatformRole> PlatformRoles => Set<PlatformRole>();
    public DbSet<PlatformPermission> PlatformPermissions => Set<PlatformPermission>();
    public DbSet<PlatformUserRole> PlatformUserRoles => Set<PlatformUserRole>();
    public DbSet<PlatformRolePermission> PlatformRolePermissions => Set<PlatformRolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("platform");

        modelBuilder.Entity<PlatformUser>(b =>
        {
            b.ToTable("PlatformUsers");
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Email).HasMaxLength(320);
            b.Property(x => x.FullName).HasMaxLength(200);
        });

        modelBuilder.Entity<PlatformRole>(b =>
        {
            b.ToTable("PlatformRoles");
            b.Property(x => x.Name).HasMaxLength(100);
            b.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<PlatformPermission>(b =>
        {
            b.ToTable("PlatformPermissions");
            b.Property(x => x.Code).HasMaxLength(128);
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<PlatformUserRole>(b =>
        {
            b.ToTable("PlatformUserRoles");
            b.HasIndex(x => new { x.PlatformUserId, x.PlatformRoleId }).IsUnique();
            b.HasOne(x => x.PlatformUser).WithMany(x => x.UserRoles).HasForeignKey(x => x.PlatformUserId);
            b.HasOne(x => x.PlatformRole).WithMany(x => x.UserRoles).HasForeignKey(x => x.PlatformRoleId);
        });

        modelBuilder.Entity<PlatformRolePermission>(b =>
        {
            b.ToTable("PlatformRolePermissions");
            b.HasIndex(x => new { x.PlatformRoleId, x.PlatformPermissionId }).IsUnique();
            b.HasOne(x => x.PlatformRole).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PlatformRoleId);
            b.HasOne(x => x.PlatformPermission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PlatformPermissionId);
        });

        modelBuilder.ApplySoftDeleteQueryFilter();
    }
}
