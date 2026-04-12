using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class ProjectService : IProjectService
{
    private readonly AppDbContext _app;

    public ProjectService(AppDbContext app)
    {
        _app = app;
    }

    public async Task<(IReadOnlyList<ProjectListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        Guid? clientId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _app.Projects.AsNoTracking()
            .Include(x => x.Client)
            .Where(x => x.SocietyId == societyId);

        if (clientId is { } cid)
            query = query.Where(x => x.ClientId == cid);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var s = pagination.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s) ||
                x.Client!.Name.ToLower().Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);

        query = pagination.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? ApplySortAsc(query, pagination.SortBy)
            : ApplySortDesc(query, pagination.SortBy);

        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new ProjectListItemDto
            {
                Id = x.Id,
                ClientId = x.ClientId,
                ClientName = x.Client!.Name,
                DealId = x.DealId,
                Name = x.Name,
                Status = x.Status,
                SystemSizeKw = x.SystemSizeKw,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return (items, pagination.ToMeta(total));
    }

    public async Task<ProjectDto> GetAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var row = await _app.Projects.AsNoTracking()
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == projectId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        return Map(row);
    }

    public async Task<ProjectDto> CreateAsync(Guid societyId, CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        await EnsureClientInSocietyAsync(societyId, request.ClientId, cancellationToken);
        if (request.DealId is { } dealId)
            await EnsureDealInSocietyAsync(societyId, dealId, cancellationToken);

        var project = new Project
        {
            SocietyId = societyId,
            ClientId = request.ClientId,
            DealId = request.DealId,
            Name = request.Name.Trim(),
            Address = request.Address?.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Planned" : request.Status.Trim(),
            SystemSizeKw = request.SystemSizeKw,
            EstimatedProduction = request.EstimatedProduction,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.Projects.Add(project);
        await _app.SaveChangesAsync(cancellationToken);

        return Map(await _app.Projects.AsNoTracking()
            .Include(x => x.Client)
            .FirstAsync(x => x.Id == project.Id, cancellationToken));
    }

    public async Task<ProjectDto> UpdateAsync(Guid societyId, Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        await EnsureClientInSocietyAsync(societyId, request.ClientId, cancellationToken);
        if (request.DealId is { } dealId)
            await EnsureDealInSocietyAsync(societyId, dealId, cancellationToken);

        var project = await _app.Projects
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == projectId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        project.ClientId = request.ClientId;
        project.DealId = request.DealId;
        project.Name = request.Name.Trim();
        project.Address = request.Address?.Trim();
        project.Status = string.IsNullOrWhiteSpace(request.Status) ? "Planned" : request.Status.Trim();
        project.SystemSizeKw = request.SystemSizeKw;
        project.EstimatedProduction = request.EstimatedProduction;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.UpdatedAt = DateTimeOffset.UtcNow;

        await _app.SaveChangesAsync(cancellationToken);

        return Map(await _app.Projects.AsNoTracking()
            .Include(x => x.Client)
            .FirstAsync(x => x.Id == projectId, cancellationToken));
    }

    public async Task DeleteAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _app.Projects.FirstOrDefaultAsync(x => x.Id == projectId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        project.IsDeleted = true;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureClientInSocietyAsync(Guid societyId, Guid clientId, CancellationToken cancellationToken)
    {
        if (!await _app.Clients.AnyAsync(x => x.Id == clientId && x.SocietyId == societyId, cancellationToken))
            throw new AppException("CLIENT_NOT_FOUND", "Client not found in this society.", 404);
    }

    private async Task EnsureDealInSocietyAsync(Guid societyId, Guid dealId, CancellationToken cancellationToken)
    {
        if (!await _app.Deals.AnyAsync(x => x.Id == dealId && x.SocietyId == societyId, cancellationToken))
            throw new AppException("DEAL_NOT_FOUND", "Deal not found in this society.", 404);
    }

    private static ProjectDto Map(Project x) => new()
    {
        Id = x.Id,
        ClientId = x.ClientId,
        ClientName = x.Client!.Name,
        DealId = x.DealId,
        Name = x.Name,
        Address = x.Address,
        Status = x.Status,
        SystemSizeKw = x.SystemSizeKw,
        EstimatedProduction = x.EstimatedProduction,
        StartDate = x.StartDate,
        EndDate = x.EndDate,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };

    private static IQueryable<Project> ApplySortAsc(IQueryable<Project> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderBy(x => x.Name),
            "status" => query.OrderBy(x => x.Status),
            "client" => query.OrderBy(x => x.Client!.Name),
            _ => query.OrderBy(x => x.CreatedAt)
        };

    private static IQueryable<Project> ApplySortDesc(IQueryable<Project> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderByDescending(x => x.Name),
            "status" => query.OrderByDescending(x => x.Status),
            "client" => query.OrderByDescending(x => x.Client!.Name),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };
}
