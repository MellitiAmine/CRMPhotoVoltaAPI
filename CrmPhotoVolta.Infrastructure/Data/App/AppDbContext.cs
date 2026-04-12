using CrmPhotoVolta.Domain.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Data.App;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectStage> ProjectStages => Set<ProjectStage>();
    public DbSet<ProjectStageTracking> ProjectStageTrackings => Set<ProjectStageTracking>();
    public DbSet<CrmTask> Tasks => Set<CrmTask>();
    public DbSet<Installation> Installations => Set<Installation>();
    public DbSet<InstallationChecklistItem> InstallationChecklistItems => Set<InstallationChecklistItem>();
    public DbSet<InstallationPhoto> InstallationPhotos => Set<InstallationPhoto>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<CalendarEvent> Events => Set<CalendarEvent>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SocietySettings> SocietySettings => Set<SocietySettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");

        modelBuilder.Entity<Lead>(b =>
        {
            b.ToTable("Leads");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Leads_SocietyId");
            b.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<LeadActivity>(b =>
        {
            b.ToTable("LeadActivities");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_LeadActivities_SocietyId");
            b.HasOne(x => x.Lead).WithMany(x => x.Activities).HasForeignKey(x => x.LeadId);
        });

        modelBuilder.Entity<Client>(b =>
        {
            b.ToTable("Clients");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Clients_SocietyId");
            b.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Deal>(b =>
        {
            b.ToTable("Deals");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Deals_SocietyId");
            b.HasOne(x => x.Lead).WithMany(x => x.Deals).HasForeignKey(x => x.LeadId);
        });

        modelBuilder.Entity<PipelineStage>(b =>
        {
            b.ToTable("PipelineStages");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_PipelineStages_SocietyId");
        });

        modelBuilder.Entity<Project>(b =>
        {
            b.ToTable("Projects");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Projects_SocietyId");
            b.Property(x => x.ProgressPercent).HasDefaultValue(0);
            b.HasOne(x => x.Client).WithMany(x => x.Projects).HasForeignKey(x => x.ClientId);
            b.HasOne(x => x.Deal).WithMany(x => x.Projects).HasForeignKey(x => x.DealId);
        });

        modelBuilder.Entity<ProjectStage>(b =>
        {
            b.ToTable("ProjectStages");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_ProjectStages_SocietyId");
        });

        modelBuilder.Entity<ProjectStageTracking>(b =>
        {
            b.ToTable("ProjectStageTracking");
            b.HasIndex(x => x.ProjectId).HasDatabaseName("IX_ProjectStageTracking_ProjectId");
            b.HasOne(x => x.Project).WithMany(x => x.StageTrackings).HasForeignKey(x => x.ProjectId);
            b.HasOne(x => x.Stage).WithMany(x => x.Trackings).HasForeignKey(x => x.StageId);
        });

        modelBuilder.Entity<CrmTask>(b =>
        {
            b.ToTable("Tasks");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Tasks_SocietyId");
            b.HasOne(x => x.Project).WithMany(x => x.Tasks).HasForeignKey(x => x.ProjectId);
        });

        modelBuilder.Entity<Installation>(b =>
        {
            b.ToTable("Installations");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Installations_SocietyId");
            b.HasOne(x => x.Project).WithMany(x => x.Installations).HasForeignKey(x => x.ProjectId);
        });

        modelBuilder.Entity<InstallationChecklistItem>(b =>
        {
            b.ToTable("InstallationChecklist");
            b.HasOne(x => x.Installation).WithMany(x => x.Checklist).HasForeignKey(x => x.InstallationId);
        });

        modelBuilder.Entity<InstallationPhoto>(b =>
        {
            b.ToTable("InstallationPhotos");
            b.HasOne(x => x.Installation).WithMany(x => x.Photos).HasForeignKey(x => x.InstallationId);
        });

        modelBuilder.Entity<Document>(b =>
        {
            b.ToTable("Documents");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Documents_SocietyId");
        });

        modelBuilder.Entity<CalendarEvent>(b =>
        {
            b.ToTable("Events");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Events_SocietyId");
        });

        modelBuilder.Entity<Quote>(b =>
        {
            b.ToTable("Quotes");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Quotes_SocietyId");
            b.HasIndex(x => new { x.SocietyId, x.QuoteNumber }).IsUnique();
            b.Property(x => x.QuoteNumber).HasMaxLength(64);
            b.Property(x => x.Title).HasMaxLength(200);
            b.Property(x => x.Status).HasMaxLength(40);
            b.Property(x => x.Currency).HasMaxLength(8);
            b.HasOne(x => x.Lead).WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Deal).WithMany().HasForeignKey(x => x.DealId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QuoteItem>(b =>
        {
            b.ToTable("QuoteItems");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_QuoteItems_SocietyId");
            b.HasIndex(x => x.QuoteId).HasDatabaseName("IX_QuoteItems_QuoteId");
            b.Property(x => x.Description).HasMaxLength(500);
            b.HasOne(x => x.Quote).WithMany(x => x.Items).HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(b =>
        {
            b.ToTable("Notifications");
            b.HasIndex(x => new { x.SocietyId, x.UserId }).HasDatabaseName("IX_Notifications_Society_User");
            b.Property(x => x.Title).HasMaxLength(200);
            b.Property(x => x.Type).HasMaxLength(64);
        });

        modelBuilder.Entity<SocietySettings>(b =>
        {
            b.ToTable("SocietySettings");
            b.HasIndex(x => x.SocietyId).IsUnique();
        });

        modelBuilder.ApplySoftDeleteQueryFilter();
    }
}
