using System.Text.Json;
using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Domain.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrmPhotoVolta.Infrastructure.Data.App;

public sealed class AppDbContext : DbContext
{
    private readonly Guid? _tenantSocietyId;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null) : base(options)
    {
        // RLS-ready: this tenant id can later be pushed as `SET app.current_society_id = ...`.
        _tenantSocietyId = tenantContext?.CurrentSocietyId;
    }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();
    public DbSet<LeadJournalEntry> LeadJournalEntries => Set<LeadJournalEntry>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTimelineEvent> ProjectTimelineEvents => Set<ProjectTimelineEvent>();
    public DbSet<ProjectStage> ProjectStages => Set<ProjectStage>();
    public DbSet<ProjectStageTracking> ProjectStageTrackings => Set<ProjectStageTracking>();
    public DbSet<CrmTask> Tasks => Set<CrmTask>();
    public DbSet<Installation> Installations => Set<Installation>();
    public DbSet<InstallationChecklistItem> InstallationChecklistItems => Set<InstallationChecklistItem>();
    public DbSet<InstallationPhoto> InstallationPhotos => Set<InstallationPhoto>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<CalendarEvent> Events => Set<CalendarEvent>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ProjectDocument> ProjectDocuments => Set<ProjectDocument>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SocietySettings> SocietySettings => Set<SocietySettings>();
    public DbSet<WhatsAppRecommendation> WhatsAppRecommendations => Set<WhatsAppRecommendation>();
    public DbSet<CommercialProfile> CommercialProfiles => Set<CommercialProfile>();
    public DbSet<TechnicienProfile> TechnicienProfiles => Set<TechnicienProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");

        var tagsConverter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
        );

        modelBuilder.Entity<Lead>(b =>
        {
            b.ToTable("Leads");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Leads_SocietyId");
            b.HasIndex(x => new { x.SocietyId, x.Lvi }).HasDatabaseName("IX_Leads_SocietyId_Lvi");
            b.Property(x => x.Name).HasMaxLength(200);
            b.Property(x => x.Temperature).HasConversion<int>();
            b.Property(x => x.Priority).HasConversion<int>();
            b.Property(x => x.Tags)
             .HasConversion(tagsConverter)
             .HasColumnType("text")
             .HasDefaultValueSql("'[]'");
        });

        modelBuilder.Entity<LeadActivity>(b =>
        {
            b.ToTable("LeadActivities");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_LeadActivities_SocietyId");
            b.HasOne(x => x.Lead).WithMany(x => x.Activities).HasForeignKey(x => x.LeadId);
            b.Property(x => x.Type).HasConversion<int>();
        });

        modelBuilder.Entity<LeadJournalEntry>(b =>
        {
            b.ToTable("LeadJournalEntries");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_LeadJournalEntries_SocietyId");
            b.HasIndex(x => new { x.LeadId, x.CreatedAt }).HasDatabaseName("IX_LeadJournalEntries_LeadId_CreatedAt");
            b.HasOne(x => x.Lead).WithMany(x => x.JournalEntries).HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.Cascade);
            b.Property(x => x.Action).HasMaxLength(120);
            b.Property(x => x.RelatedEntityType).HasMaxLength(80);
            b.Property(x => x.MetadataJson).HasColumnType("text");
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
            b.HasIndex(x => new { x.SocietyId, x.LeadId })
                .IsUnique()
                .HasDatabaseName("IX_Projects_SocietyId_LeadId")
                .HasFilter("\"LeadId\" IS NOT NULL");
            b.Property(x => x.ProgressPercent).HasDefaultValue(0);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Priority).HasConversion<int>();
            b.Property(x => x.Reference).HasMaxLength(64);
            b.Property(x => x.Notes).HasMaxLength(4000);
            b.Property(x => x.RoofType).HasMaxLength(120);
            b.Property(x => x.InstallationType).HasMaxLength(120);
            b.Property(x => x.SystemSizeKw).HasPrecision(18, 3);
            b.Property(x => x.EstimatedProduction).HasPrecision(18, 3);
            b.Property(x => x.TotalHt).HasPrecision(18, 3);
            b.Property(x => x.TotalTva).HasPrecision(18, 3);
            b.Property(x => x.TotalTtc).HasPrecision(18, 3);
            b.Property(x => x.EstimatedMargin).HasPrecision(18, 3);
            b.HasOne(x => x.Client).WithMany(x => x.Projects).HasForeignKey(x => x.ClientId);
            b.HasOne(x => x.Deal).WithMany(x => x.Projects).HasForeignKey(x => x.DealId);
            b.HasOne(x => x.Lead).WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Quote).WithMany().HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProjectTimelineEvent>(b =>
        {
            b.ToTable("ProjectTimelineEvents");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_ProjectTimelineEvents_SocietyId");
            b.HasIndex(x => x.ProjectId).HasDatabaseName("IX_ProjectTimelineEvents_ProjectId");
            b.Property(x => x.Type).HasConversion<int>();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.HasOne(x => x.Project).WithMany(x => x.TimelineEvents).HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectStage>(b =>
        {
            b.ToTable("ProjectStages");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_ProjectStages_SocietyId");
        });

        modelBuilder.Entity<ProjectStageTracking>(b =>
        {
            b.ToTable("ProjectStageTracking");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_ProjectStageTracking_SocietyId");
            b.HasIndex(x => x.ProjectId).HasDatabaseName("IX_ProjectStageTracking_ProjectId");
            b.Property(x => x.Status).HasConversion<string>();
            b.HasOne(x => x.Project).WithMany(x => x.StageTrackings).HasForeignKey(x => x.ProjectId);
            b.HasOne(x => x.Stage).WithMany(x => x.Trackings).HasForeignKey(x => x.StageId);
        });

        modelBuilder.Entity<CrmTask>(b =>
        {
            b.ToTable("Tasks");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Tasks_SocietyId");
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Priority).HasConversion<int>();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.HasOne(x => x.Project).WithMany(x => x.Tasks).HasForeignKey(x => x.ProjectId);
        });

        modelBuilder.Entity<Installation>(b =>
        {
            b.ToTable("Installations");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Installations_SocietyId");
            b.Property(x => x.Status).HasConversion<string>();
            b.HasOne(x => x.Project).WithMany(x => x.Installations).HasForeignKey(x => x.ProjectId);
        });

        modelBuilder.Entity<InstallationChecklistItem>(b =>
        {
            b.ToTable("InstallationChecklist");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_InstallationChecklist_SocietyId");
            b.HasOne(x => x.Installation).WithMany(x => x.Checklist).HasForeignKey(x => x.InstallationId);
        });

        modelBuilder.Entity<InstallationPhoto>(b =>
        {
            b.ToTable("InstallationPhotos");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_InstallationPhotos_SocietyId");
            b.HasOne(x => x.Installation).WithMany(x => x.Photos).HasForeignKey(x => x.InstallationId);
        });

        modelBuilder.Entity<Document>(b =>
        {
            b.ToTable("Documents");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Documents_SocietyId");
        });

        var participantsConverter = new ValueConverter<List<Guid>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
        );

        modelBuilder.Entity<CalendarEvent>(b =>
        {
            b.ToTable("Events");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Events_SocietyId");
            b.HasIndex(x => x.LeadId).HasDatabaseName("IX_Events_LeadId");
            b.Property(x => x.Title).HasMaxLength(300);
            b.Property(x => x.Type).HasMaxLength(40);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Participants)
             .HasConversion(participantsConverter)
             .HasColumnType("text")
             .HasDefaultValueSql("'[]'");
            b.HasOne(x => x.Lead).WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Item>(b =>
        {
            b.ToTable("Items");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Items_SocietyId");
            b.Property(x => x.Name).HasMaxLength(200);
            b.Property(x => x.Reference).HasMaxLength(120);
            b.Property(x => x.Unit).HasMaxLength(32);
            b.Property(x => x.DefaultPrice).HasPrecision(18, 3);
            b.Property(x => x.TvaRate).HasPrecision(5, 2);
        });

        modelBuilder.Entity<Quote>(b =>
        {
            b.ToTable("Quotes");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Quotes_SocietyId");
            b.HasIndex(x => new { x.SocietyId, x.QuoteNumber }).IsUnique();
            b.Property(x => x.QuoteNumber).HasMaxLength(64);
            b.Property(x => x.Title).HasMaxLength(200);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            b.Property(x => x.Currency).HasMaxLength(8);
            b.Property(x => x.TotalAmount).HasPrecision(18, 3);
            b.Property(x => x.TotalHt).HasPrecision(18, 3);
            b.Property(x => x.TotalTva).HasPrecision(18, 3);
            b.Property(x => x.TotalTtc).HasPrecision(18, 3);
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
            b.Property(x => x.Quantity).HasPrecision(18, 2);
            b.Property(x => x.UnitPrice).HasPrecision(18, 3);
            b.Property(x => x.Discount).HasPrecision(5, 2);
            b.Property(x => x.TvaRate).HasPrecision(5, 2);
            b.Property(x => x.TotalHt).HasPrecision(18, 3);
            b.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Quote).WithMany(x => x.Items).HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Contract>(b =>
        {
            b.ToTable("Contracts");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Contracts_SocietyId");
            b.HasIndex(x => x.ProjectId).HasDatabaseName("IX_Contracts_ProjectId");
            b.Property(x => x.Reference).HasMaxLength(64);
            b.Property(x => x.Notes).HasMaxLength(4000);
            b.Property(x => x.PdfUrl).HasMaxLength(1000);
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            b.Property(x => x.TotalAmount).HasPrecision(18, 3);
            b.HasOne(x => x.Project).WithMany(x => x.Contracts).HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invoice>(b =>
        {
            b.ToTable("Invoices");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Invoices_SocietyId");
            b.HasIndex(x => x.ProjectId).HasDatabaseName("IX_Invoices_ProjectId");
            b.HasIndex(x => new { x.SocietyId, x.Reference }).IsUnique()
                .HasDatabaseName("IX_Invoices_SocietyId_Reference");
            b.Property(x => x.Reference).HasMaxLength(64);
            b.Property(x => x.Notes).HasMaxLength(4000);
            b.Property(x => x.PdfUrl).HasMaxLength(1000);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            b.Property(x => x.TotalHt).HasPrecision(18, 3);
            b.Property(x => x.TotalTva).HasPrecision(18, 3);
            b.Property(x => x.TotalTtc).HasPrecision(18, 3);
            b.Property(x => x.PaidAmount).HasPrecision(18, 3);
            b.Ignore(x => x.RemainingAmount);
            b.HasOne(x => x.Project).WithMany(x => x.Invoices).HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceItem>(b =>
        {
            b.ToTable("InvoiceItems");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_InvoiceItems_SocietyId");
            b.HasIndex(x => x.InvoiceId).HasDatabaseName("IX_InvoiceItems_InvoiceId");
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.Quantity).HasPrecision(18, 2);
            b.Property(x => x.UnitPrice).HasPrecision(18, 3);
            b.Property(x => x.TvaRate).HasPrecision(5, 2);
            b.Property(x => x.TotalHt).HasPrecision(18, 3);
            b.HasOne(x => x.Invoice).WithMany(x => x.Items).HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Payment>(b =>
        {
            b.ToTable("Payments");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_Payments_SocietyId");
            b.HasIndex(x => x.InvoiceId).HasDatabaseName("IX_Payments_InvoiceId");
            b.Property(x => x.Amount).HasPrecision(18, 3);
            b.Property(x => x.Method).HasConversion<string>().HasMaxLength(40);
            b.Property(x => x.Reference).HasMaxLength(100);
            b.Property(x => x.Notes).HasMaxLength(1000);
            b.HasOne(x => x.Invoice).WithMany(x => x.Payments).HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectDocument>(b =>
        {
            b.ToTable("ProjectDocuments");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_ProjectDocuments_SocietyId");
            b.HasIndex(x => x.ProjectId).HasDatabaseName("IX_ProjectDocuments_ProjectId");
            b.Property(x => x.Name).HasMaxLength(300);
            b.Property(x => x.Url).HasMaxLength(1000);
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            b.HasOne(x => x.Project).WithMany(x => x.ProjectDocuments).HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<WhatsAppRecommendation>(b =>
        {
            b.ToTable("WhatsAppRecommendations");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_WhatsAppRecommendations_SocietyId");
            b.HasIndex(x => x.LeadId).HasDatabaseName("IX_WhatsAppRecommendations_LeadId");
            b.Property(x => x.PhoneNumber).HasMaxLength(64);
            b.Property(x => x.Message).HasMaxLength(4000);
            b.Property(x => x.ActionType).HasConversion<int>();
            b.Property(x => x.Priority).HasConversion<int>();
            b.Property(x => x.Temperature).HasConversion<int>();
            b.HasOne(x => x.Lead).WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CommercialProfile>(b =>
        {
            b.ToTable("CommercialProfiles");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_CommercialProfiles_SocietyId");
            b.HasIndex(x => new { x.SocietyId, x.UserId }).IsUnique()
              .HasDatabaseName("IX_CommercialProfiles_SocietyId_UserId");
            b.HasIndex(x => new { x.SocietyId, x.EmployeeId }).IsUnique()
              .HasDatabaseName("IX_CommercialProfiles_SocietyId_EmployeeId");
            b.Property(x => x.FirstName).HasMaxLength(100);
            b.Property(x => x.LastName).HasMaxLength(100);
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.Phone).HasMaxLength(30);
            b.Property(x => x.EmployeeId).HasMaxLength(50);
            b.Property(x => x.Department).HasMaxLength(120);
            b.Property(x => x.Position).HasMaxLength(120);
            b.Property(x => x.ContractType).HasMaxLength(30);
            b.Property(x => x.WorkTime).HasMaxLength(20);
            b.Property(x => x.Status).HasMaxLength(20);
            b.Property(x => x.StartDate).HasMaxLength(10);
            b.Property(x => x.ScoreTier).HasMaxLength(20);
            b.Property(x => x.ScoreTrend).HasMaxLength(10);
            b.Property(x => x.Salary).HasPrecision(18, 2);
            b.Property(x => x.MonthlyTarget).HasPrecision(18, 2);
            b.Property(x => x.KpiRevenueGenerated).HasPrecision(18, 2);
        });

        modelBuilder.Entity<TechnicienProfile>(b =>
        {
            b.ToTable("TechnicienProfiles");
            b.HasIndex(x => x.SocietyId).HasDatabaseName("IX_TechnicienProfiles_SocietyId");
            b.HasIndex(x => new { x.SocietyId, x.UserId }).IsUnique()
              .HasDatabaseName("IX_TechnicienProfiles_SocietyId_UserId");
            b.HasIndex(x => new { x.SocietyId, x.EmployeeId }).IsUnique()
              .HasDatabaseName("IX_TechnicienProfiles_SocietyId_EmployeeId");
            b.Property(x => x.FirstName).HasMaxLength(100);
            b.Property(x => x.LastName).HasMaxLength(100);
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.Phone).HasMaxLength(30);
            b.Property(x => x.EmployeeId).HasMaxLength(50);
            b.Property(x => x.Department).HasMaxLength(120);
            b.Property(x => x.Position).HasMaxLength(120);
            b.Property(x => x.ContractType).HasMaxLength(30);
            b.Property(x => x.WorkTime).HasMaxLength(20);
            b.Property(x => x.Status).HasMaxLength(20);
            b.Property(x => x.StartDate).HasMaxLength(10);
            b.Property(x => x.ScoreTier).HasMaxLength(20);
            b.Property(x => x.ScoreTrend).HasMaxLength(10);
            b.Property(x => x.Salary).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Lead>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<LeadActivity>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<LeadJournalEntry>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<Client>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<Deal>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<PipelineStage>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<Project>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<ProjectTimelineEvent>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<ProjectStage>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<ProjectStageTracking>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<CrmTask>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<Installation>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<InstallationChecklistItem>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<InstallationPhoto>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<Document>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<CalendarEvent>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<Item>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<Quote>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<QuoteItem>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<Contract>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<Invoice>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<InvoiceItem>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<Payment>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<ProjectDocument>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<Notification>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<SocietySettings>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<WhatsAppRecommendation>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<CommercialProfile>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
        modelBuilder.Entity<TechnicienProfile>().HasQueryFilter(x => !x.IsDeleted && (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
    }
}
