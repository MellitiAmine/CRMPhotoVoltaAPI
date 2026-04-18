using CrmPhotoVolta.Domain.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Data.Core;

public sealed class CoreDbContext : DbContext
{
    public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Society> Societies => Set<Society>();
    public DbSet<UserSociety> UserSocieties => Set<UserSociety>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("core");

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Email).HasMaxLength(320);
            b.Property(x => x.FullName).HasMaxLength(200);
            b.Property(x => x.Phone).HasMaxLength(50);
        });

        modelBuilder.Entity<Society>(b =>
        {
            b.ToTable("Societies");
            b.Property(x => x.Name).HasMaxLength(200);
            b.HasOne(x => x.SubscriptionPlan)
                .WithMany(x => x.Societies)
                .HasForeignKey(x => x.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserSociety>(b =>
        {
            b.ToTable("UserSocieties");
            b.HasIndex(x => new { x.UserId, x.SocietyId }).IsUnique();
            b.HasOne(x => x.User).WithMany(x => x.UserSocieties).HasForeignKey(x => x.UserId);
            b.HasOne(x => x.Society).WithMany(x => x.UserSocieties).HasForeignKey(x => x.SocietyId);
            b.HasOne(x => x.Role).WithMany(x => x.UserSocieties).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<Role>(b =>
        {
            b.ToTable("Roles");
            b.Property(x => x.Name).HasMaxLength(100);
            b.Property(x => x.RoleType).HasConversion<int>();
            b.HasIndex(x => x.SocietyId);
            b.HasIndex(x => new { x.SocietyId, x.Name }).IsUnique();
            b.HasOne(x => x.Society).WithMany().HasForeignKey(x => x.SocietyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Permission>(b =>
        {
            b.ToTable("Permissions");
            b.Property(x => x.Code).HasMaxLength(128);
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.ToTable("RolePermissions");
            b.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
            b.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId);
            b.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId);
        });

        modelBuilder.Entity<SubscriptionPlan>(b =>
        {
            b.ToTable("SubscriptionPlans");
            b.Property(x => x.Name).HasMaxLength(120);
            b.Property(x => x.Code).HasMaxLength(64);
            b.Property(x => x.Currency).HasMaxLength(8);
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Subscription>(b =>
        {
            b.ToTable("Subscriptions");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Subscriptions_SocietyId");
            b.HasOne(x => x.Society).WithMany(x => x.Subscriptions).HasForeignKey(x => x.SocietyId);
            b.HasOne(x => x.Plan).WithMany(x => x.Subscriptions).HasForeignKey(x => x.PlanId);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("RefreshTokens");
            b.HasIndex(x => x.Token).IsUnique();
            b.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId);
        });

        modelBuilder.ApplySoftDeleteQueryFilter();
    }
}
