using CrmPhotoVolta.Application.Crm.Contracts;
using CrmPhotoVolta.Application.Crm.Invoices;
using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Storage;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class ProjectDetailService : IProjectDetailService
{
    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;
    private readonly IFileStorageService _files;

    public ProjectDetailService(AppDbContext app, CoreDbContext core, IFileStorageService files)
    {
        _app = app;
        _core = core;
        _files = files;
    }

    public async Task<ProjectDetailDto> GetDetailAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _app.Projects
            .AsNoTracking()
            .Include(p => p.Client)
            .Include(p => p.Lead)
            .Include(p => p.Quote)
            .Include(p => p.Tasks)
            .Include(p => p.TimelineEvents)
            .Include(p => p.Contracts)
            .Include(p => p.Invoices).ThenInclude(i => i.Payments)
            .Include(p => p.ProjectDocuments)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        // Resolve user names from Core DB
        var userIds = new HashSet<Guid>();
        if (project.CommercialUserId.HasValue) userIds.Add(project.CommercialUserId.Value);
        if (project.TechnicianUserId.HasValue) userIds.Add(project.TechnicianUserId.Value);
        if (project.ManagerUserId.HasValue) userIds.Add(project.ManagerUserId.Value);
        foreach (var t in project.Tasks)
            if (t.AssignedToUserId.HasValue) userIds.Add(t.AssignedToUserId.Value);
        foreach (var e in project.TimelineEvents)
            if (e.CreatedByUserId.HasValue) userIds.Add(e.CreatedByUserId.Value);
        foreach (var d in project.ProjectDocuments)
            if (d.UploadedByUserId.HasValue) userIds.Add(d.UploadedByUserId.Value);

        var userNames = new Dictionary<Guid, string>();
        var userEmails = new Dictionary<Guid, string?>();

        if (userIds.Any())
        {
            var userRecords = await _core.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync(cancellationToken);

            foreach (var u in userRecords)
            {
                userNames[u.Id] = u.FullName;
                userEmails[u.Id] = u.Email;
            }
        }

        string UserName(Guid? id) => id.HasValue && userNames.TryGetValue(id.Value, out var n) ? n : string.Empty;

        ProjectUserRefDto? BuildUserRef(Guid? id) => id.HasValue && userNames.TryGetValue(id.Value, out var n)
            ? new ProjectUserRefDto
            {
                Id = id.Value,
                FullName = n,
                Email = userEmails.GetValueOrDefault(id.Value)
            }
            : null;

        var financial = BuildFinancial(project);

        return new ProjectDetailDto
        {
            Id = project.Id,
            Name = project.Name,
            Reference = project.Reference,
            Address = project.Address,
            Status = project.Status,
            Priority = project.Priority,
            Notes = project.Notes,
            ProgressPercent = project.ProgressPercent,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            ExpectedInstallationDate = project.ExpectedInstallationDate,
            LastActivityAt = project.LastActivityAt,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            SystemSizeKw = project.SystemSizeKw,
            EstimatedProduction = project.EstimatedProduction,
            RoofType = project.RoofType,
            InstallationType = project.InstallationType,
            PanelCount = project.PanelCount,
            InverterCount = project.InverterCount,
            Client = project.Client is null ? null : new ProjectClientRefDto
            {
                Id = project.Client.Id,
                Name = project.Client.Name,
                Phone = project.Client.Phone,
                Email = project.Client.Email,
                Address = project.Client.Address
            },
            Lead = project.Lead is null ? null : new ProjectLeadRefDto
            {
                Id = project.Lead.Id,
                Name = project.Lead.Name,
                Status = project.Lead.Status,
                Lvi = project.Lead.Lvi,
                Sd = project.Lead.Sd
            },
            Quote = project.Quote is null ? null : new ProjectQuoteRefDto
            {
                Id = project.Quote.Id,
                QuoteNumber = project.Quote.QuoteNumber,
                Title = project.Quote.Title,
                Status = project.Quote.Status,
                TotalTtc = project.Quote.TotalTtc,
                AcceptedAt = project.Quote.AcceptedAt
            },
            Commercial = BuildUserRef(project.CommercialUserId),
            Technician = BuildUserRef(project.TechnicianUserId),
            Manager = BuildUserRef(project.ManagerUserId),
            Tasks = project.Tasks
                .OrderBy(t => t.DueDate)
                .Select(t => new ProjectTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedToName = UserName(t.AssignedToUserId),
                    DueDate = t.DueDate,
                    CompletedAt = t.CompletedAt,
                    CreatedAt = t.CreatedAt
                })
                .ToList(),
            Timeline = project.TimelineEvents
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new ProjectTimelineEventDto
                {
                    Id = e.Id,
                    Type = e.Type,
                    Description = e.Description,
                    CreatedByUserId = e.CreatedByUserId,
                    CreatedByName = UserName(e.CreatedByUserId),
                    CreatedAt = e.CreatedAt
                })
                .ToList(),
            Documents = project.ProjectDocuments
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new ProjectDocumentDto
                {
                    Id = d.Id,
                    Type = d.Type,
                    Name = d.Name,
                    Url = _files.ToAbsoluteUrl(d.Url),
                    FileName = Path.GetFileName(d.Url),
                    UploadedByUserId = d.UploadedByUserId,
                    UploadedByName = UserName(d.UploadedByUserId),
                    UploadedAt = d.UploadedAt
                })
                .ToList(),
            Contracts = project.Contracts
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ContractSummaryDto
                {
                    Id = c.Id,
                    Reference = c.Reference,
                    Type = c.Type,
                    Status = c.Status,
                    TotalAmount = c.TotalAmount,
                    SignedAt = c.SignedAt,
                    PdfUrl = c.PdfUrl
                })
                .ToList(),
            Invoices = project.Invoices
                .OrderByDescending(i => i.InvoiceDate)
                .Select(i => new InvoiceSummaryDto
                {
                    Id = i.Id,
                    Reference = i.Reference,
                    Status = i.Status,
                    InvoiceDate = i.InvoiceDate,
                    DueDate = i.DueDate,
                    TotalTtc = i.TotalTtc,
                    PaidAmount = i.PaidAmount,
                    RemainingAmount = i.RemainingAmount,
                    PdfUrl = i.PdfUrl
                })
                .ToList(),
            Financial = financial
        };
    }

    private static FinancialSummaryDto BuildFinancial(Project project)
    {
        var quoteTtc = project.Quote?.TotalTtc ?? project.TotalTtc;
        var totalInvoiced = project.Invoices.Sum(i => i.TotalTtc);
        var totalPaid = project.Invoices.Sum(i => i.PaidAmount);
        var totalRemaining = totalInvoiced - totalPaid;
        var margin = project.EstimatedMargin ?? 0m;
        var marginPct = quoteTtc > 0 ? Math.Round(margin / quoteTtc * 100, 2) : 0m;

        return new FinancialSummaryDto
        {
            QuoteTotalTtc = quoteTtc,
            TotalInvoiced = totalInvoiced,
            TotalPaid = totalPaid,
            TotalRemaining = totalRemaining,
            EstimatedMargin = margin,
            MarginPercent = marginPct,
            FullyPaid = totalInvoiced > 0 && totalRemaining <= 0
        };
    }
}
