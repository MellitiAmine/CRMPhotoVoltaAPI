namespace CrmPhotoVolta.Application.Roles.Dtos;

public sealed class RoleDto
{
    public Guid Id { get; init; }
    public Guid? SocietyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsSystemRole { get; init; }
}

public sealed class CreateRoleRequest
{
    public string Name { get; init; } = string.Empty;
}

public sealed class UpdateRoleRequest
{
    public string Name { get; init; } = string.Empty;
}

public sealed class ReplaceRolePermissionsRequest
{
    public IReadOnlyList<Guid> PermissionIds { get; init; } = Array.Empty<Guid>();
}
