namespace CrmPhotoVolta.Application.Permissions.Dtos;

public sealed class PermissionDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
}
