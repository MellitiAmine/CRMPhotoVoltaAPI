using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Clients;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class ClientService : IClientService
{
    private static readonly ProjectStatus[] TerminalProjectStatuses =
        [ProjectStatus.Completed, ProjectStatus.Cancelled, ProjectStatus.Done];

    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;

    public ClientService(AppDbContext app, CoreDbContext core)
    {
        _app = app;
        _core = core;
    }

    public async Task<(IReadOnlyList<ClientListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        ClientListQuery query,
        CancellationToken cancellationToken = default)
    {
        var clientQuery = _app.Clients.AsNoTracking().Where(x => x.SocietyId == societyId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLowerInvariant();
            clientQuery = clientQuery.Where(x =>
                x.Name.ToLower().Contains(s) ||
                (x.Email != null && x.Email.ToLower().Contains(s)) ||
                (x.Phone != null && x.Phone.Contains(s)));
        }

        var stats = await LoadClientStatsAsync(societyId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.Activity))
        {
            var wantActive = query.Activity.Equals("active", StringComparison.OrdinalIgnoreCase);
            var wantInactive = query.Activity.Equals("inactive", StringComparison.OrdinalIgnoreCase);
            var activeList = stats.ActiveClientIds.ToList();

            if (wantActive)
                clientQuery = clientQuery.Where(c => activeList.Contains(c.Id));
            else if (wantInactive)
                clientQuery = clientQuery.Where(c => !activeList.Contains(c.Id));
        }

        var total = await clientQuery.CountAsync(cancellationToken);
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        clientQuery = query.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? ApplySortAsc(clientQuery, query.SortBy)
            : ApplySortDesc(clientQuery, query.SortBy);

        var rows = await clientQuery
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        var items = rows.Select(c =>
        {
            stats.ByClient.TryGetValue(c.Id, out var st);
            return new ClientListItemDto
            {
                Id                 = c.Id,
                Name               = c.Name,
                Email              = c.Email,
                Phone              = c.Phone,
                Address            = c.Address,
                UserId             = c.UserId,
                IsActive           = stats.ActiveClientIds.Contains(c.Id),
                ProjectCount       = st?.ProjectCount ?? 0,
                ActiveProjectCount = st?.ActiveProjectCount ?? 0,
                TotalInvoicedTtc   = st?.TotalInvoicedTtc ?? 0,
                TotalPaid          = st?.TotalPaid ?? 0,
                TotalRemaining     = st?.TotalRemaining ?? 0,
                LastActivityAt     = st?.LastActivityAt,
                CreatedAt          = c.CreatedAt
            };
        }).ToList();

        var meta = new PaginationMeta
        {
            Page       = page,
            PageSize   = size,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)size),
            HasNext    = page * size < total,
            HasPrevious = page > 1
        };

        return (items, meta);
    }

    public async Task<ClientDto> GetAsync(Guid societyId, Guid clientId, CancellationToken cancellationToken = default)
    {
        var row = await RequireClientAsync(societyId, clientId, cancellationToken);
        return Map(row);
    }

    public async Task<Client360Dto> Get360Async(Guid societyId, Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await RequireClientAsync(societyId, clientId, cancellationToken);

        var projects = await _app.Projects.AsNoTracking()
            .Where(p => p.SocietyId == societyId && p.ClientId == clientId)
            .OrderByDescending(p => p.LastActivityAt ?? p.UpdatedAt ?? p.CreatedAt)
            .ToListAsync(cancellationToken);

        var projectIds = projects.Select(p => p.Id).ToList();

        var invoices = await _app.Invoices.AsNoTracking()
            .Where(i => i.SocietyId == societyId && i.ClientId == clientId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

        var installations = projectIds.Count == 0
            ? []
            : await _app.Installations.AsNoTracking()
                .Include(i => i.Project)
                .Where(i => i.SocietyId == societyId && projectIds.Contains(i.ProjectId))
                .OrderByDescending(i => i.Date)
                .ToListAsync(cancellationToken);

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var payments = invoiceIds.Count == 0
            ? new List<Payment>()
            : await _app.Payments.AsNoTracking()
                .Where(p => p.SocietyId == societyId && invoiceIds.Contains(p.InvoiceId))
                .OrderByDescending(p => p.PaidOn)
                .ToListAsync(cancellationToken);

        var userIds = projects
            .SelectMany(p => new[] { p.CommercialUserId, p.TechnicianUserId, p.ManagerUserId })
            .Concat(installations.Select(i => (Guid?)i.TechnicianId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var userNames = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await ProjectUserNameResolver.LoadNamesAsync(_core, userIds.Select(id => (Guid?)id), cancellationToken);

        var projectNameById = projects.ToDictionary(p => p.Id, p => p.Name);
        var invoiceById = invoices.ToDictionary(i => i.Id);

        var stats = await LoadClientStatsAsync(societyId, cancellationToken);
        stats.ByClient.TryGetValue(clientId, out var st);

        return new Client360Dto
        {
            Id        = client.Id,
            Name      = client.Name,
            Phone     = client.Phone,
            Email     = client.Email,
            Address   = client.Address,
            UserId    = client.UserId,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt,
            Summary = new Client360SummaryDto
            {
                IsActive           = stats.ActiveClientIds.Contains(clientId),
                ProjectCount       = st?.ProjectCount ?? projects.Count,
                ActiveProjectCount = st?.ActiveProjectCount ?? projects.Count(p => IsActiveProject(p.Status)),
                InstallationCount  = installations.Count,
                InvoiceCount       = invoices.Count(i => i.Status != InvoiceStatus.Cancelled),
                PaymentCount       = payments.Count,
                TotalInvoicedTtc   = st?.TotalInvoicedTtc ?? 0,
                TotalPaid          = st?.TotalPaid ?? 0,
                TotalRemaining     = st?.TotalRemaining ?? 0,
                LastActivityAt     = st?.LastActivityAt
            },
            Projects = projects.Select(p => new Client360ProjectDto
            {
                Id               = p.Id,
                Name             = p.Name,
                Reference        = p.Reference,
                Status           = p.Status,
                TotalTtc         = p.TotalTtc,
                ProgressPercent  = p.ProgressPercent,
                CommercialName   = ProjectUserNameResolver.Resolve(userNames, p.CommercialUserId),
                TechnicianName   = ProjectUserNameResolver.Resolve(userNames, p.TechnicianUserId),
                LastActivityAt   = p.LastActivityAt ?? p.UpdatedAt,
                CreatedAt        = p.CreatedAt
            }).ToList(),
            Invoices = invoices.Select(i => new Client360InvoiceDto
            {
                Id              = i.Id,
                ProjectId       = i.ProjectId,
                ProjectName     = projectNameById.GetValueOrDefault(i.ProjectId),
                Reference       = i.Reference,
                Status          = i.Status,
                InvoiceDate     = i.InvoiceDate,
                TotalTtc        = i.TotalTtc,
                PaidAmount      = i.PaidAmount,
                RemainingAmount = i.TotalTtc - i.PaidAmount
            }).ToList(),
            Installations = installations.Select(i => new Client360InstallationDto
            {
                Id             = i.Id,
                ProjectId      = i.ProjectId,
                ProjectName    = i.Project?.Name ?? projectNameById.GetValueOrDefault(i.ProjectId),
                TechnicianId   = i.TechnicianId,
                TechnicianName = ProjectUserNameResolver.Resolve(userNames, i.TechnicianId),
                Date           = i.Date,
                Status         = i.Status,
                CreatedAt      = i.CreatedAt
            }).ToList(),
            Payments = payments.Select(p =>
            {
                var inv = invoiceById[p.InvoiceId];
                return new Client360PaymentDto
                {
                    Id               = p.Id,
                    InvoiceId        = p.InvoiceId,
                    InvoiceReference = inv.Reference,
                    ProjectId        = inv.ProjectId,
                    ProjectName      = projectNameById.GetValueOrDefault(inv.ProjectId),
                    Amount           = p.Amount,
                    PaidOn           = p.PaidOn,
                    Method           = p.Method,
                    Reference        = p.Reference,
                    CreatedAt        = p.CreatedAt
                };
            }).ToList()
        };
    }

    public async Task<ClientDto> CreateAsync(Guid societyId, CreateClientRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        if (request.UserId is { } uid)
            await EnsureUserInSocietyAsync(societyId, uid, cancellationToken);

        var client = new Client
        {
            SocietyId = societyId,
            Name      = request.Name.Trim(),
            Phone     = request.Phone?.Trim(),
            Email     = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(),
            Address   = request.Address?.Trim(),
            UserId    = request.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.Clients.Add(client);
        await _app.SaveChangesAsync(cancellationToken);

        return Map(await _app.Clients.AsNoTracking().FirstAsync(x => x.Id == client.Id, cancellationToken));
    }

    public async Task<ClientDto> UpdateAsync(Guid societyId, Guid clientId, UpdateClientRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("VALIDATION_ERROR", "Name is required.", 400);

        if (request.UserId is { } uid)
            await EnsureUserInSocietyAsync(societyId, uid, cancellationToken);

        var client = await _app.Clients.FirstOrDefaultAsync(x => x.Id == clientId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("CLIENT_NOT_FOUND", "Client not found.", 404);

        client.Name    = request.Name.Trim();
        client.Phone   = request.Phone?.Trim();
        client.Email   = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        client.Address = request.Address?.Trim();
        client.UserId  = request.UserId;
        client.UpdatedAt = DateTimeOffset.UtcNow;

        await _app.SaveChangesAsync(cancellationToken);

        return Map(await _app.Clients.AsNoTracking().FirstAsync(x => x.Id == clientId, cancellationToken));
    }

    public async Task DeleteAsync(Guid societyId, Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await _app.Clients.FirstOrDefaultAsync(x => x.Id == clientId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("CLIENT_NOT_FOUND", "Client not found.", 404);

        var hasProjects = await _app.Projects.AnyAsync(x => x.ClientId == clientId && x.SocietyId == societyId, cancellationToken);
        if (hasProjects)
            throw new AppException("CLIENT_HAS_PROJECTS", "Cannot delete a client that still has projects.", 409);

        client.IsDeleted = true;
        client.UpdatedAt = DateTimeOffset.UtcNow;
        await _app.SaveChangesAsync(cancellationToken);
    }

    private async Task<ClientStatsSnapshot> LoadClientStatsAsync(Guid societyId, CancellationToken ct)
    {
        var projectRows = await _app.Projects.AsNoTracking()
            .Where(p => p.SocietyId == societyId)
            .Select(p => new
            {
                p.ClientId,
                p.Status,
                ActivityAt = p.LastActivityAt ?? p.UpdatedAt ?? p.CreatedAt
            })
            .ToListAsync(ct);

        var invoiceRows = await _app.Invoices.AsNoTracking()
            .Where(i => i.SocietyId == societyId && i.Status != InvoiceStatus.Cancelled)
            .Select(i => new
            {
                i.ClientId,
                i.TotalTtc,
                i.PaidAmount,
                HasOpenBalance = i.PaidAmount < i.TotalTtc,
                ActivityAt = i.UpdatedAt ?? i.CreatedAt
            })
            .ToListAsync(ct);

        var byClient = new Dictionary<Guid, ClientAggregateStats>();
        var activeIds = new HashSet<Guid>();

        foreach (var g in projectRows.GroupBy(p => p.ClientId))
        {
            var activeCount = g.Count(p => IsActiveProject(p.Status));
            if (activeCount > 0)
                activeIds.Add(g.Key);

            byClient[g.Key] = new ClientAggregateStats
            {
                ProjectCount       = g.Count(),
                ActiveProjectCount = activeCount,
                LastActivityAt     = g.Max(p => p.ActivityAt)
            };
        }

        foreach (var g in invoiceRows.GroupBy(i => i.ClientId))
        {
            if (g.Any(i => i.HasOpenBalance))
                activeIds.Add(g.Key);

            if (!byClient.TryGetValue(g.Key, out var st))
            {
                st = new ClientAggregateStats();
                byClient[g.Key] = st;
            }

            st.TotalInvoicedTtc = g.Sum(i => i.TotalTtc);
            st.TotalPaid        = g.Sum(i => i.PaidAmount);
            st.TotalRemaining   = st.TotalInvoicedTtc - st.TotalPaid;

            var invoiceLast = g.Max(i => i.ActivityAt);
            if (st.LastActivityAt is null || invoiceLast > st.LastActivityAt)
                st.LastActivityAt = invoiceLast;
        }

        return new ClientStatsSnapshot(byClient, activeIds);
    }

    private static bool IsActiveProject(ProjectStatus status) =>
        !TerminalProjectStatuses.Contains(status);

    private async Task<Client> RequireClientAsync(Guid societyId, Guid clientId, CancellationToken ct) =>
        await _app.Clients.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == clientId && x.SocietyId == societyId, ct)
        ?? throw new AppException("CLIENT_NOT_FOUND", "Client not found.", 404);

    private async Task EnsureUserInSocietyAsync(Guid societyId, Guid userId, CancellationToken cancellationToken)
    {
        var ok = await _core.UserSocieties.AnyAsync(
            x => x.UserId == userId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken);

        if (!ok)
            throw new AppException("USER_NOT_IN_SOCIETY", "The selected user is not a member of this society.", 400);
    }

    private static ClientDto Map(Client x) => new()
    {
        Id        = x.Id,
        Name      = x.Name,
        Phone     = x.Phone,
        Email     = x.Email,
        Address   = x.Address,
        UserId    = x.UserId,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };

    private static IQueryable<Client> ApplySortAsc(IQueryable<Client> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name"  => query.OrderBy(x => x.Name),
            "email" => query.OrderBy(x => x.Email),
            "phone" => query.OrderBy(x => x.Phone),
            _       => query.OrderBy(x => x.CreatedAt)
        };

    private static IQueryable<Client> ApplySortDesc(IQueryable<Client> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name"  => query.OrderByDescending(x => x.Name),
            "email" => query.OrderByDescending(x => x.Email),
            "phone" => query.OrderByDescending(x => x.Phone),
            _       => query.OrderByDescending(x => x.CreatedAt)
        };

    private sealed class ClientAggregateStats
    {
        public int ProjectCount { get; set; }
        public int ActiveProjectCount { get; set; }
        public decimal TotalInvoicedTtc { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
        public DateTimeOffset? LastActivityAt { get; set; }
    }

    private sealed record ClientStatsSnapshot(
        Dictionary<Guid, ClientAggregateStats> ByClient,
        HashSet<Guid> ActiveClientIds);
}
