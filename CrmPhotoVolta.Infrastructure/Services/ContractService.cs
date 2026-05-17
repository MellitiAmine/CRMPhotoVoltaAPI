using CrmPhotoVolta.Application.Crm.Contracts;
using CrmPhotoVolta.Application.Crm.Notifications;
using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class ContractService : IContractService
{
    private readonly AppDbContext _app;
    private readonly INotificationService _notifications;

    public ContractService(AppDbContext app, INotificationService notifications)
    {
        _app = app;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<ContractDto>> ListByProjectAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var rows = await _app.Contracts.AsNoTracking()
            .Include(c => c.Client)
            .Where(c => c.ProjectId == projectId && c.SocietyId == societyId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<ContractDto> GetAsync(Guid societyId, Guid contractId, CancellationToken cancellationToken = default)
    {
        var row = await _app.Contracts.AsNoTracking()
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == contractId && c.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("CONTRACT_NOT_FOUND", "Contract not found.", 404);

        return Map(row);
    }

    public async Task<ContractDto> CreateAsync(Guid societyId, Guid actorUserId,
        CreateContractRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reference))
            throw new AppException("VALIDATION_ERROR", "Reference is required.", 400);

        var project = await _app.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        var clientId = ProjectFinancialGuard.ResolveClientId(request.ClientId, project.ClientId);

        var now = DateTimeOffset.UtcNow;
        var contract = new Contract
        {
            SocietyId = societyId,
            ProjectId = request.ProjectId,
            ClientId = clientId,
            Reference = request.Reference.Trim(),
            Type = request.Type,
            Status = ContractStatus.Draft,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalAmount = request.TotalAmount,
            Notes = request.Notes?.Trim(),
            PdfUrl = request.PdfUrl?.Trim(),
            CreatedAt = now,
            CreatedById = actorUserId
        };
        _app.Contracts.Add(contract);

        _app.ProjectTimelineEvents.Add(new ProjectTimelineEvent
        {
            SocietyId = societyId,
            ProjectId = request.ProjectId,
            Type = ProjectTimelineEventType.ContractGenerated,
            Description = $"Contrat «{contract.Reference}» généré ({contract.Type}).",
            CreatedByUserId = actorUserId,
            CreatedAt = now
        });

        await _app.SaveChangesAsync(cancellationToken);

        if (project.CommercialUserId.HasValue)
        {
            await _notifications.NotifyUserAsync(
                societyId, project.CommercialUserId.Value,
                "ContractGenerated",
                $"Contrat {contract.Reference} créé",
                $"Un contrat de type {contract.Type} a été créé pour le projet {project.Name}.",
                cancellationToken);
        }

        return await GetAsync(societyId, contract.Id, cancellationToken);
    }

    public async Task<ContractDto> UpdateAsync(Guid societyId, Guid contractId,
        UpdateContractRequest request, CancellationToken cancellationToken = default)
    {
        var contract = await _app.Contracts
            .FirstOrDefaultAsync(c => c.Id == contractId && c.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("CONTRACT_NOT_FOUND", "Contract not found.", 404);

        contract.Status = request.Status;
        contract.SignedAt = request.SignedAt;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.TotalAmount = request.TotalAmount;
        contract.Notes = request.Notes?.Trim();
        contract.PdfUrl = request.PdfUrl?.Trim();
        contract.UpdatedAt = DateTimeOffset.UtcNow;

        await _app.SaveChangesAsync(cancellationToken);
        return await GetAsync(societyId, contractId, cancellationToken);
    }

    private static ContractDto Map(Contract c) => new()
    {
        Id = c.Id,
        ProjectId = c.ProjectId,
        ClientId = c.ClientId,
        ClientName = c.Client?.Name ?? string.Empty,
        Reference = c.Reference,
        Type = c.Type,
        Status = c.Status,
        SignedAt = c.SignedAt,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        TotalAmount = c.TotalAmount,
        Notes = c.Notes,
        PdfUrl = c.PdfUrl,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
