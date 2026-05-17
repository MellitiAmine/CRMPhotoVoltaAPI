using CrmPhotoVolta.Application.Crm.Invoices;
using CrmPhotoVolta.Application.Crm.Notifications;
using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _app;
    private readonly INotificationService _notifications;

    public InvoiceService(AppDbContext app, INotificationService notifications)
    {
        _app = app;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<InvoiceDto>> ListByProjectAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var rows = await _app.Invoices.AsNoTracking()
            .Include(i => i.Client)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.ProjectId == projectId && i.SocietyId == societyId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<InvoiceDto> GetAsync(Guid societyId, Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var row = await _app.Invoices.AsNoTracking()
            .Include(i => i.Client)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("INVOICE_NOT_FOUND", "Invoice not found.", 404);

        return Map(row);
    }

    public async Task<InvoiceDto> CreateAsync(Guid societyId, Guid actorUserId,
        CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reference))
            throw new AppException("VALIDATION_ERROR", "Reference is required.", 400);

        var duplicateRef = await _app.Invoices.AnyAsync(
            i => i.SocietyId == societyId && i.Reference == request.Reference.Trim(),
            cancellationToken);
        if (duplicateRef)
            throw new AppException("DUPLICATE_REFERENCE", "Invoice reference already exists.", 409);

        var project = await _app.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        var clientId = ProjectFinancialGuard.ResolveClientId(request.ClientId, project.ClientId);
        var invoiceDate = request.InvoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var now = DateTimeOffset.UtcNow;
        var invoice = new Invoice
        {
            SocietyId = societyId,
            ProjectId = request.ProjectId,
            ClientId = clientId,
            Reference = request.Reference.Trim(),
            Status = InvoiceStatus.Draft,
            InvoiceDate = invoiceDate,
            DueDate = request.DueDate,
            Notes = request.Notes?.Trim(),
            PdfUrl = request.PdfUrl?.Trim(),
            CreatedAt = now,
            CreatedById = actorUserId
        };

        // Calculate line totals
        foreach (var line in request.Items)
        {
            var totalHt = Math.Round(line.Quantity * line.UnitPrice, 3);
            invoice.Items.Add(new InvoiceItem
            {
                SocietyId = societyId,
                ItemId = line.ItemId,
                Description = line.Description.Trim(),
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                TvaRate = line.TvaRate,
                TotalHt = totalHt,
                CreatedAt = now
            });
        }

        invoice.TotalHt = invoice.Items.Sum(x => x.TotalHt);
        invoice.TotalTva = Math.Round(invoice.Items.Sum(x => x.TotalHt * x.TvaRate / 100), 3);
        invoice.TotalTtc = invoice.TotalHt + invoice.TotalTva;

        _app.Invoices.Add(invoice);

        _app.ProjectTimelineEvents.Add(new ProjectTimelineEvent
        {
            SocietyId = societyId,
            ProjectId = request.ProjectId,
            Type = ProjectTimelineEventType.InvoiceGenerated,
            Description = $"Facture «{invoice.Reference}» générée ({invoice.TotalTtc:N3} TND TTC).",
            CreatedByUserId = actorUserId,
            CreatedAt = now
        });

        await _app.SaveChangesAsync(cancellationToken);

        if (project.CommercialUserId.HasValue)
        {
            await _notifications.NotifyUserAsync(
                societyId, project.CommercialUserId.Value,
                "InvoiceGenerated",
                $"Facture {invoice.Reference} créée",
                $"Facture de {invoice.TotalTtc:N3} TND générée pour le projet {project.Name}.",
                cancellationToken);
        }

        return await GetAsync(societyId, invoice.Id, cancellationToken);
    }

    public async Task<InvoiceDto> UpdateAsync(Guid societyId, Guid invoiceId,
        UpdateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await _app.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("INVOICE_NOT_FOUND", "Invoice not found.", 404);

        invoice.Status = request.Status;
        invoice.DueDate = request.DueDate;
        invoice.Notes = request.Notes?.Trim();
        invoice.PdfUrl = request.PdfUrl?.Trim();
        invoice.UpdatedAt = DateTimeOffset.UtcNow;

        await _app.SaveChangesAsync(cancellationToken);
        return await GetAsync(societyId, invoiceId, cancellationToken);
    }

    public async Task<InvoiceDto> AddPaymentAsync(
        Guid societyId, Guid invoiceId, Guid actorUserId,
        AddPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await _app.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("INVOICE_NOT_FOUND", "Invoice not found.", 404);

        if (request.Amount <= 0)
            throw new AppException("VALIDATION_ERROR", "Payment amount must be positive.", 400);

        var now = DateTimeOffset.UtcNow;
        invoice.Payments.Add(new Payment
        {
            SocietyId = societyId,
            Amount = request.Amount,
            PaidOn = request.PaidOn,
            Method = request.Method,
            Reference = request.Reference?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedAt = now,
            CreatedById = actorUserId
        });

        invoice.PaidAmount = invoice.Payments.Sum(p => p.Amount);
        invoice.Status = invoice.PaidAmount >= invoice.TotalTtc
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;
        invoice.UpdatedAt = now;

        // Add payment timeline event on invoice's project
        _app.ProjectTimelineEvents.Add(new ProjectTimelineEvent
        {
            SocietyId = societyId,
            ProjectId = invoice.ProjectId,
            Type = ProjectTimelineEventType.PaymentReceived,
            Description = $"Paiement de {request.Amount:N3} TND reçu (facture {invoice.Reference}).",
            CreatedByUserId = actorUserId,
            CreatedAt = now
        });

        await _app.SaveChangesAsync(cancellationToken);

        // If fully paid, notify commercial
        if (invoice.Status == InvoiceStatus.Paid)
        {
            var project = await _app.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == invoice.ProjectId, cancellationToken);

            if (project?.CommercialUserId.HasValue == true)
            {
                await _notifications.NotifyUserAsync(
                    societyId, project.CommercialUserId.Value,
                    "InvoicePaid",
                    $"Facture {invoice.Reference} intégralement payée",
                    $"Montant total: {invoice.TotalTtc:N3} TND",
                    cancellationToken);
            }
        }

        return await GetAsync(societyId, invoiceId, cancellationToken);
    }

    public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _app.Projects.AsNoTracking()
            .Include(p => p.Quote)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        var invoices = await _app.Invoices.AsNoTracking()
            .Include(i => i.Payments)
            .Where(i => i.ProjectId == projectId && i.SocietyId == societyId && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var quoteTtc = project.Quote?.TotalTtc ?? project.TotalTtc;
        var totalInvoiced = invoices.Sum(i => i.TotalTtc);
        var totalPaid = invoices.Sum(i => i.PaidAmount);
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

    private static InvoiceDto Map(Invoice i) => new()
    {
        Id = i.Id,
        ProjectId = i.ProjectId,
        ClientId = i.ClientId,
        ClientName = i.Client?.Name ?? string.Empty,
        Reference = i.Reference,
        Status = i.Status,
        InvoiceDate = i.InvoiceDate,
        DueDate = i.DueDate,
        TotalHt = i.TotalHt,
        TotalTva = i.TotalTva,
        TotalTtc = i.TotalTtc,
        PaidAmount = i.PaidAmount,
        RemainingAmount = i.RemainingAmount,
        PdfUrl = i.PdfUrl,
        Notes = i.Notes,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt,
        Items = i.Items.Select(x => new InvoiceItemDto
        {
            Id = x.Id,
            ItemId = x.ItemId,
            Description = x.Description,
            Quantity = x.Quantity,
            UnitPrice = x.UnitPrice,
            TvaRate = x.TvaRate,
            TotalHt = x.TotalHt
        }).ToList(),
        Payments = i.Payments.OrderBy(p => p.PaidOn).Select(p => new PaymentDto
        {
            Id = p.Id,
            Amount = p.Amount,
            PaidOn = p.PaidOn,
            Method = p.Method,
            Reference = p.Reference,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt
        }).ToList()
    };
}
