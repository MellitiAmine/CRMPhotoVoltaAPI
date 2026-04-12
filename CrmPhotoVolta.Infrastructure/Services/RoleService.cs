using CrmPhotoVolta.Application.Exceptions;
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
}
