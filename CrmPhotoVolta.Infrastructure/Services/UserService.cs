using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Users;
using CrmPhotoVolta.Application.Users.Dtos;
using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly CoreDbContext _db;

    public UserService(CoreDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<UserListItemDto> Items, PaginationMeta Meta)> ListPagedAsync(
        Guid societyId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _db.UserSocieties
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Role)
            .Where(x => x.SocietyId == societyId && !x.IsDeleted && !x.User!.IsDeleted);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var s = pagination.Search.Trim().ToLower();
            query = query.Where(x =>
                x.User!.Email.ToLower().Contains(s) ||
                x.User.FullName.ToLower().Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);

        query = pagination.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? ApplySortAsc(query, pagination.SortBy)
            : ApplySortDesc(query, pagination.SortBy);

        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new UserListItemDto
            {
                Id = x.UserId,
                Email = x.User!.Email,
                FullName = x.User.FullName,
                Phone = x.User.Phone,
                IsActive = x.User.IsActive,
                RoleId = x.RoleId,
                RoleName = x.Role!.Name,
                CreatedAt = x.User.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return (items, pagination.ToMeta(total));
    }

    private static IQueryable<UserSociety> ApplySortAsc(IQueryable<UserSociety> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "email" => query.OrderBy(x => x.User!.Email),
            "fullname" => query.OrderBy(x => x.User!.FullName),
            "id" => query.OrderBy(x => x.UserId),
            _ => query.OrderBy(x => x.User!.CreatedAt)
        };

    private static IQueryable<UserSociety> ApplySortDesc(IQueryable<UserSociety> query, string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "email" => query.OrderByDescending(x => x.User!.Email),
            "fullname" => query.OrderByDescending(x => x.User!.FullName),
            "id" => query.OrderByDescending(x => x.UserId),
            _ => query.OrderByDescending(x => x.User!.CreatedAt)
        };

    public async Task<UserDetailDto> GetAsync(Guid societyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await _db.UserSocieties
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.SocietyId == societyId && x.UserId == userId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("USER_NOT_FOUND", "User not found in this society.", 404);

        return MapDetail(row);
    }

    public async Task<UserDetailDto> CreateAsync(Guid societyId, Guid actorUserId, CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Email.ToLower() == email, cancellationToken))
            throw new AppException("EMAIL_IN_USE", "Email is already registered.", 409);

        var role = await _db.Roles.FirstOrDefaultAsync(
            x => x.Id == request.RoleId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken)
            ?? throw new AppException("ROLE_NOT_FOUND", "Role not found in this society.", 404);

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Phone = request.Phone,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = actorUserId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        _db.UserSocieties.Add(new UserSociety
        {
            UserId = user.Id,
            SocietyId = societyId,
            RoleId = role.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = actorUserId
        });

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(societyId, user.Id, cancellationToken);
    }

    public async Task<UserDetailDto> UpdateAsync(Guid societyId, Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var row = await _db.UserSocieties
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.SocietyId == societyId && x.UserId == userId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("USER_NOT_FOUND", "User not found in this society.", 404);

        row.User!.FullName = request.FullName.Trim();
        row.User.Phone = request.Phone;
        row.User.IsActive = request.IsActive;
        row.User.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(societyId, userId, cancellationToken);
    }

    public async Task DeleteAsync(Guid societyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await _db.UserSocieties
            .FirstOrDefaultAsync(x => x.SocietyId == societyId && x.UserId == userId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("USER_NOT_FOUND", "User not found in this society.", 404);

        row.IsDeleted = true;
        row.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignRoleAsync(Guid societyId, Guid userId, AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        var row = await _db.UserSocieties
            .FirstOrDefaultAsync(x => x.SocietyId == societyId && x.UserId == userId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("USER_NOT_FOUND", "User not found in this society.", 404);

        var role = await _db.Roles.FirstOrDefaultAsync(
            x => x.Id == request.RoleId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken)
            ?? throw new AppException("ROLE_NOT_FOUND", "Role not found in this society.", 404);

        row.RoleId = role.Id;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static UserDetailDto MapDetail(UserSociety x) => new()
    {
        Id = x.UserId,
        Email = x.User!.Email,
        FullName = x.User.FullName,
        Phone = x.User.Phone,
        IsActive = x.User.IsActive,
        RoleId = x.RoleId,
        RoleName = x.Role!.Name,
        CreatedAt = x.User.CreatedAt
    };
}
