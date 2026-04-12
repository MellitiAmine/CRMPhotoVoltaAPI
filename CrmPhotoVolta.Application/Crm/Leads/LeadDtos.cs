namespace CrmPhotoVolta.Application.Crm.Leads;

public sealed class LeadListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class LeadDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateLeadRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Status { get; init; }
    public Guid? AssignedToUserId { get; init; }
}

public sealed class UpdateLeadRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
}

public sealed class LeadActivityDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class AddLeadActivityRequest
{
    public string Type { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
