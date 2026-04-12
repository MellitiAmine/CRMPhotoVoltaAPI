namespace CrmPhotoVolta.Application.Crm.Clients;

public sealed class ClientListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public Guid? UserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ClientDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public Guid? UserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateClientRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public Guid? UserId { get; init; }
}

public sealed class UpdateClientRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public Guid? UserId { get; init; }
}
