using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class ProjectService : IProjectService
{
    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;

    public ProjectService(AppDbContext app, CoreDbContext core)
    {
        _app = app;
        _core = core;
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
                x.Client!.Name.ToLower().Contains(s) ||
                (x.Reference != null && x.Reference.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);

        query = pagination.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? ApplySortAsc(query, pagination.SortBy)
            : ApplySortDesc(query, pagination.SortBy);

        var rows = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var userNames = await ProjectUserNameResolver.LoadNamesAsync(
            _core,
            rows.SelectMany(p => new Guid?[] { p.CommercialUserId, p.ManagerUserId, p.TechnicianUserId }),
            cancellationToken);

        var items = rows.Select(p => MapListItem(p, userNames)).ToList();
        return (items, pagination.ToMeta(total));
    }

    public async Task<ProjectDto> GetAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var row = await _app.Projects.AsNoTracking()
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == projectId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        var userNames = await ProjectUserNameResolver.LoadNamesAsync(
            _core,
            new[] { row.CommercialUserId, row.ManagerUserId, row.TechnicianUserId },
            cancellationToken);

        return Map(row, userNames);
    }

    public async Task<ProjectDto> CreateAsync(Guid societyId, CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        await EnsureClientInSocietyAsync(societyId, request.ClientId, cancellationToken);
        if (request.DealId is { } dealId)
            await EnsureDealInSocietyAsync(societyId, dealId, cancellationToken);

        var reference = string.IsNullOrWhiteSpace(request.Reference)
            ? await NextProjectReferenceAsync(societyId, cancellationToken)
            : request.Reference.Trim();

        var project = new Project
        {
            SocietyId = societyId,
            ClientId = request.ClientId,
            LeadId = request.LeadId,
            DealId = request.DealId,
            Name = request.Name.Trim(),
            Reference = reference,
            Address = request.Address?.Trim(),
            Status = request.Status ?? ProjectStatus.New,
            Priority = request.Priority ?? LeadPriority.Low,
            SystemSizeKw = request.SystemSizeKw,
            EstimatedProduction = request.EstimatedProduction,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ExpectedInstallationDate = request.ExpectedInstallationDate,
            ManagerUserId = request.ManagerUserId,
            CommercialUserId = request.CommercialUserId,
            TechnicianUserId = request.TechnicianUserId,
            ProgressPercent = Math.Clamp(request.ProgressPercent, 0, 100),
            LastActivityAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (project.ManagerUserId is { } m)
            await EnsureUserInSocietyAsync(societyId, m, cancellationToken);
        if (project.CommercialUserId is { } c)
            await EnsureUserInSocietyAsync(societyId, c, cancellationToken);
        if (project.TechnicianUserId is { } t)
            await EnsureUserInSocietyAsync(societyId, t, cancellationToken);

        _app.Projects.Add(project);
        await _app.SaveChangesAsync(cancellationToken);

        return await GetAsync(societyId, project.Id, cancellationToken);
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
        project.LeadId = request.LeadId;
        project.DealId = request.DealId;
        project.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Reference))
            project.Reference = request.Reference.Trim();
        project.Address = request.Address?.Trim();
        project.Status = request.Status;
        project.Priority = request.Priority;
        project.Notes = request.Notes?.Trim();
        project.SystemSizeKw = request.SystemSizeKw;
        project.EstimatedProduction = request.EstimatedProduction;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.ExpectedInstallationDate = request.ExpectedInstallationDate;
        project.ManagerUserId = request.ManagerUserId;
        project.CommercialUserId = request.CommercialUserId;
        project.TechnicianUserId = request.TechnicianUserId;
        project.ProgressPercent = Math.Clamp(request.ProgressPercent, 0, 100);
        project.LastActivityAt = DateTimeOffset.UtcNow;
        project.UpdatedAt = DateTimeOffset.UtcNow;

        if (project.ManagerUserId is { } m2)
            await EnsureUserInSocietyAsync(societyId, m2, cancellationToken);
        if (project.CommercialUserId is { } c2)
            await EnsureUserInSocietyAsync(societyId, c2, cancellationToken);
        if (project.TechnicianUserId is { } t2)
            await EnsureUserInSocietyAsync(societyId, t2, cancellationToken);

        await _app.SaveChangesAsync(cancellationToken);

        return await GetAsync(societyId, projectId, cancellationToken);
    }

    public async Task DeleteAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _app.Projects.FirstOrDefaultAsync(x => x.Id == projectId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        project.IsDeleted = true;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectOverviewDto> GetOverviewAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await GetAsync(societyId, projectId, cancellationToken);

        var openTasks = await _app.Tasks.CountAsync(
            x => x.SocietyId == societyId && x.ProjectId == projectId && x.Status != CrmTaskStatus.Done,
            cancellationToken);

        var inst = await _app.Installations
            .Where(x => x.SocietyId == societyId && x.ProjectId == projectId)
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);

        return new ProjectOverviewDto
        {
            Project = project,
            OpenTasks = openTasks,
            InstallationsTotal = inst.Count,
            InstallationsCompleted = inst.Count(s => s == InstallationStatus.Completed)
        };
    }

    public async Task<ProjectProgressDto> GetProgressAsync(Guid societyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        _ = await _app.Projects.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == projectId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        var track = await _app.ProjectStageTrackings
            .Where(x => x.ProjectId == projectId && x.SocietyId == societyId)
            .ToListAsync(cancellationToken);

        var total = track.Count;
        var done = track.Count(x => x.Status == ProjectStageTrackingStatus.Done || x.CompletedAt is not null);

        var project = await _app.Projects.AsNoTracking()
            .FirstAsync(x => x.Id == projectId && x.SocietyId == societyId, cancellationToken);

        return new ProjectProgressDto
        {
            ProgressPercent = project.ProgressPercent,
            StageTrackingsCompleted = done,
            StageTrackingsTotal = total
        };
    }

    public async Task<ProjectDto> AssignTechnicianAsync(
        Guid societyId,
        Guid projectId,
        AssignProjectUserRequest request,
        CancellationToken cancellationToken = default) =>
        await AssignUserAsync(societyId, projectId, request.UserId, cancellationToken, assignTechnician: true);

    public async Task<ProjectDto> AssignManagerAsync(
        Guid societyId,
        Guid projectId,
        AssignProjectUserRequest request,
        CancellationToken cancellationToken = default) =>
        await AssignUserAsync(societyId, projectId, request.UserId, cancellationToken, assignManager: true);

    public async Task<ProjectDto> AssignCommercialAsync(
        Guid societyId,
        Guid projectId,
        AssignProjectUserRequest request,
        CancellationToken cancellationToken = default) =>
        await AssignUserAsync(societyId, projectId, request.UserId, cancellationToken, assignCommercial: true);

    public async Task<ProjectDto> UpdateProgressAsync(
        Guid societyId,
        Guid projectId,
        PatchProjectProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        var project = await _app.Projects.FirstOrDefaultAsync(x => x.Id == projectId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        project.ProgressPercent = Math.Clamp(request.ProgressPercent, 0, 100);
        project.LastActivityAt = DateTimeOffset.UtcNow;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);

        return await GetAsync(societyId, projectId, cancellationToken);
    }

    private async Task<ProjectDto> AssignUserAsync(
        Guid societyId,
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken,
        bool assignTechnician = false,
        bool assignManager = false,
        bool assignCommercial = false)
    {
        await EnsureUserInSocietyAsync(societyId, userId, cancellationToken);

        var project = await _app.Projects.FirstOrDefaultAsync(x => x.Id == projectId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("PROJECT_NOT_FOUND", "Project not found.", 404);

        if (assignTechnician) project.TechnicianUserId = userId;
        if (assignManager) project.ManagerUserId = userId;
        if (assignCommercial) project.CommercialUserId = userId;

        project.LastActivityAt = DateTimeOffset.UtcNow;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);

        return await GetAsync(societyId, projectId, cancellationToken);
    }

    private async Task<string> NextProjectReferenceAsync(Guid societyId, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"PRJ-{year}-";
        var existing = await _app.Projects
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.SocietyId == societyId && x.Reference != null && x.Reference.StartsWith(prefix))
            .Select(x => x.Reference!)
            .ToListAsync(cancellationToken);

        return SequentialReferenceGenerator.Next(prefix, existing);
    }

    private async Task EnsureUserInSocietyAsync(Guid societyId, Guid userId, CancellationToken cancellationToken)
    {
        var ok = await _core.UserSocieties.AnyAsync(
            x => x.UserId == userId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken);

        if (!ok)
            throw new AppException("USER_NOT_IN_SOCIETY", "The selected user is not a member of this society.", 400);
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

    private static ProjectListItemDto MapListItem(Project x, IReadOnlyDictionary<Guid, string> userNames) => new()
    {
        Id = x.Id,
        ClientId = x.ClientId,
        ClientName = x.Client!.Name,
        LeadId = x.LeadId,
        DealId = x.DealId,
        Name = x.Name,
        Reference = x.Reference,
        Status = x.Status,
        Priority = x.Priority,
        TotalTtc = x.TotalTtc,
        SystemSizeKw = x.SystemSizeKw,
        ProgressPercent = x.ProgressPercent,
        CommercialUserId = x.CommercialUserId,
        CommercialName = ProjectUserNameResolver.Resolve(userNames, x.CommercialUserId),
        ExpectedInstallationDate = x.ExpectedInstallationDate,
        LastActivityAt = x.LastActivityAt,
        CreatedAt = x.CreatedAt
    };

    private static ProjectDto Map(Project x, IReadOnlyDictionary<Guid, string> userNames) => new()
    {
        Id = x.Id,
        ClientId = x.ClientId,
        ClientName = x.Client!.Name,
        LeadId = x.LeadId,
        QuoteId = x.QuoteId,
        DealId = x.DealId,
        Name = x.Name,
        Reference = x.Reference,
        Address = x.Address,
        Status = x.Status,
        Priority = x.Priority,
        Notes = x.Notes,
        TotalHt = x.TotalHt,
        TotalTva = x.TotalTva,
        TotalTtc = x.TotalTtc,
        SystemSizeKw = x.SystemSizeKw,
        EstimatedProduction = x.EstimatedProduction,
        StartDate = x.StartDate,
        EndDate = x.EndDate,
        ExpectedInstallationDate = x.ExpectedInstallationDate,
        ManagerUserId = x.ManagerUserId,
        ManagerName = ProjectUserNameResolver.Resolve(userNames, x.ManagerUserId),
        CommercialUserId = x.CommercialUserId,
        CommercialName = ProjectUserNameResolver.Resolve(userNames, x.CommercialUserId),
        TechnicianUserId = x.TechnicianUserId,
        TechnicianName = ProjectUserNameResolver.Resolve(userNames, x.TechnicianUserId),
        ProgressPercent = x.ProgressPercent,
        LastActivityAt = x.LastActivityAt,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };

    private static IQueryable<Project> ApplySortAsc(IQueryable<Project> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderBy(x => x.Name),
            "reference" => query.OrderBy(x => x.Reference),
            "status" => query.OrderBy(x => x.Status),
            "priority" => query.OrderBy(x => x.Priority),
            "totalttc" => query.OrderBy(x => x.TotalTtc),
            "client" => query.OrderBy(x => x.Client!.Name),
            _ => query.OrderBy(x => x.CreatedAt)
        };

    private static IQueryable<Project> ApplySortDesc(IQueryable<Project> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderByDescending(x => x.Name),
            "reference" => query.OrderByDescending(x => x.Reference),
            "status" => query.OrderByDescending(x => x.Status),
            "priority" => query.OrderByDescending(x => x.Priority),
            "totalttc" => query.OrderByDescending(x => x.TotalTtc),
            "client" => query.OrderByDescending(x => x.Client!.Name),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };
}
