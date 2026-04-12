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
    private readonly AppDbContext _app;
    private readonly CoreDbContext _core;

    public ClientService(AppDbContext app, CoreDbContext core)
    {
        _app = app;
        _core = core;
    }

    public async Task<(IReadOnlyList<ClientListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _app.Clients.AsNoTracking().Where(x => x.SocietyId == societyId);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var s = pagination.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s) ||
                (x.Email != null && x.Email.ToLower().Contains(s)) ||
                (x.Phone != null && x.Phone.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);

        query = pagination.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? ApplySortAsc(query, pagination.SortBy)
            : ApplySortDesc(query, pagination.SortBy);

        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new ClientListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                UserId = x.UserId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return (items, pagination.ToMeta(total));
    }

    public async Task<ClientDto> GetAsync(Guid societyId, Guid clientId, CancellationToken cancellationToken = default)
    {
        var row = await _app.Clients.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == clientId && x.SocietyId == societyId, cancellationToken)
            ?? throw new AppException("CLIENT_NOT_FOUND", "Client not found.", 404);

        return Map(row);
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
            Name = request.Name.Trim(),
            Phone = request.Phone?.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(),
            Address = request.Address?.Trim(),
            UserId = request.UserId,
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

        client.Name = request.Name.Trim();
        client.Phone = request.Phone?.Trim();
        client.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        client.Address = request.Address?.Trim();
        client.UserId = request.UserId;
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
        Id = x.Id,
        Name = x.Name,
        Phone = x.Phone,
        Email = x.Email,
        Address = x.Address,
        UserId = x.UserId,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };

    private static IQueryable<Client> ApplySortAsc(IQueryable<Client> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderBy(x => x.Name),
            "email" => query.OrderBy(x => x.Email),
            _ => query.OrderBy(x => x.CreatedAt)
        };

    private static IQueryable<Client> ApplySortDesc(IQueryable<Client> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderByDescending(x => x.Name),
            "email" => query.OrderByDescending(x => x.Email),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };
}
