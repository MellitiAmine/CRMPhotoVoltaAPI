namespace CrmPhotoVolta.Application.Abstractions;

public interface IPlatformCurrentUser
{
    Guid? PlatformUserId { get; }
    string? Email { get; }
    IReadOnlyList<string> RoleNames { get; }
}
