namespace CrmPhotoVolta.Application.Users.Dtos;

public class UserListItemDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class UserDetailDto : UserListItemDto
{
}

public sealed class CreateUserRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public Guid RoleId { get; init; }
}

public sealed class UpdateUserRequest
{
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class AssignRoleRequest
{
    public Guid RoleId { get; init; }
}
