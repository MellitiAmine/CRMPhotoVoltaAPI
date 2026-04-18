using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Application.Permissions.Dtos;
using CrmPhotoVolta.Application.Roles;
using CrmPhotoVolta.Application.Roles.Dtos;
using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class RoleService : IRoleService
{
    private readonly CoreDbContext _db;

    public RoleService(CoreDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RoleDto>> ListAsync(Guid societyId, CancellationToken cancellationToken = default)
    {
        return await _db.Roles
            .AsNoTracking()
            .Where(x => !x.IsDeleted && (x.SocietyId == societyId || (x.SocietyId == null && x.IsSystemRole)))
            .OrderBy(x => x.Name)
            .Select(x => new RoleDto
            {
                Id = x.Id,
                SocietyId = x.SocietyId,
                Name = x.Name,
                RoleType = x.RoleType,
                IsSystemRole = x.IsSystemRole
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleDto> CreateAsync(Guid societyId, CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = new Role
        {
            SocietyId = societyId,
            Name = request.Name.Trim(),
            RoleType = RoleType.Unknown,
            IsSystemRole = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);

        return new RoleDto
        {
            Id = role.Id,
            SocietyId = role.SocietyId,
            Name = role.Name,
            RoleType = role.RoleType,
            IsSystemRole = role.IsSystemRole
        };
    }

    public async Task<RoleDto> UpdateAsync(Guid societyId, Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(
            x => x.Id == roleId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken)
            ?? throw new AppException("ROLE_NOT_FOUND", "Role not found.", 404);

        role.Name = request.Name.Trim();
        role.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new RoleDto
        {
            Id = role.Id,
            SocietyId = role.SocietyId,
            Name = role.Name,
            RoleType = role.RoleType,
            IsSystemRole = role.IsSystemRole
        };
    }

    public async Task DeleteAsync(Guid societyId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(
            x => x.Id == roleId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken)
            ?? throw new AppException("ROLE_NOT_FOUND", "Role not found.", 404);

        if (role.IsSystemRole)
            throw new AppException("ROLE_PROTECTED", "System roles cannot be deleted.", 409);

        var inUse = await _db.UserSocieties.AnyAsync(
            x => x.RoleId == roleId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken);

        if (inUse)
            throw new AppException("ROLE_IN_USE", "Role is assigned to users.", 409);

        role.IsDeleted = true;
        role.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(
        Guid societyId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        _ = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == roleId && x.SocietyId == societyId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("ROLE_NOT_FOUND", "Role not found.", 404);

        return await _db.RolePermissions.AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .Include(x => x.Permission)
            .Select(x => new PermissionDto
            {
                Id = x.Permission!.Id,
                Code = x.Permission.Code,
                Description = x.Permission.Description
            })
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplacePermissionsAsync(
        Guid societyId,
        Guid roleId,
        ReplaceRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(
            x => x.Id == roleId && x.SocietyId == societyId && !x.IsDeleted,
            cancellationToken)
            ?? throw new AppException("ROLE_NOT_FOUND", "Role not found.", 404);

        if (role.IsSystemRole)
            throw new AppException("ROLE_PROTECTED", "System roles cannot be modified this way.", 409);

        var existing = await _db.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(cancellationToken);
        _db.RolePermissions.RemoveRange(existing);

        foreach (var pid in request.PermissionIds.Distinct())
        {
            if (!await _db.Permissions.AnyAsync(x => x.Id == pid && !x.IsDeleted, cancellationToken))
                continue;

            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = pid,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        role.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
