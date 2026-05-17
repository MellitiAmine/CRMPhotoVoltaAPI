using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Leads;

public sealed class LeadListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public double? MonthlyBillEur { get; init; }
    public double? EstimatedKw { get; init; }
    public double? MontantEstimé { get; init; }
    public double? Lvi { get; init; }
    public double? Sd { get; init; }
    public DateTimeOffset? ScoredAt { get; init; }
    public LeadTemperature Temperature { get; init; }
    public LeadPriority Priority { get; init; }
    public List<string> Tags { get; init; } = new();
}

public sealed class LeadScoreBreakdownDto
{
    public double Interaction { get; init; }
    public double Intention { get; init; }
    public double Satisfaction { get; init; }
    public double Activity { get; init; }
    public double Potential { get; init; }
    public double Penalties { get; init; }
}

public sealed class LeadRecommendationDto
{
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ActionLabel { get; init; } = string.Empty;
    public string Priority { get; init; } = "Medium";
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
    public double? MonthlyBillEur { get; init; }
    public double? EstimatedKw { get; init; }
    public double? MontantEstimé { get; init; }
    public double AverageRating { get; init; }
    public bool BonusQuoteRequested { get; init; }
    public bool BonusBudgetConfirmed { get; init; }
    public bool BonusDecisionMaker { get; init; }
    public bool BonusFinancingInterest { get; init; }
    public double? Lvi { get; init; }
    public double? Sd { get; init; }
    public DateTimeOffset? ScoredAt { get; init; }
    public LeadScoreBreakdownDto? ScoreBreakdown { get; init; }
    public List<LeadRecommendationDto> Recommendations { get; init; } = new();
    public LeadTemperature Temperature { get; init; }
    public LeadPriority Priority { get; init; }
    public List<string> Tags { get; init; } = new();
}

public sealed class CreateLeadRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Status { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public double? MonthlyBillEur { get; init; }
    public double? EstimatedKw { get; init; }
    public double? MontantEstimé { get; init; }
    public double? AverageRating { get; init; }
    public bool? BonusQuoteRequested { get; init; }
    public bool? BonusBudgetConfirmed { get; init; }
    public bool? BonusDecisionMaker { get; init; }
    public bool? BonusFinancingInterest { get; init; }
}

public sealed class UpdateLeadRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
    public double? MonthlyBillEur { get; init; }
    public double? EstimatedKw { get; init; }
    public double? MontantEstimé { get; init; }
    public double? AverageRating { get; init; }
    public bool? BonusQuoteRequested { get; init; }
    public bool? BonusBudgetConfirmed { get; init; }
    public bool? BonusDecisionMaker { get; init; }
    public bool? BonusFinancingInterest { get; init; }
}

public sealed class LeadActivityDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public LeadActivityType Type { get; init; }
    public string? Notes { get; init; }
    public int? Rating { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class AddLeadActivityRequest
{
    public required LeadActivityType Type { get; init; }
    public string? Notes { get; init; }
    public int? Rating { get; init; }
}

public sealed class UpdateLeadActivityRequest
{
    public required LeadActivityType Type { get; init; }
    public string? Notes { get; init; }
    public int? Rating { get; init; }
}

public sealed class AssignLeadRequest
{
    public Guid UserId { get; init; }
}

public sealed class ConvertLeadRequest
{
    public bool CreateDeal { get; init; }
    public string? DealTitle { get; init; }
}

public sealed class ConvertLeadResultDto
{
    public required LeadDto Lead { get; init; }
    public Guid ClientId { get; init; }
    public Guid? DealId { get; init; }
}

public sealed class AddLeadNoteRequest
{
    public string Body { get; init; } = string.Empty;
}

public sealed class LeadTimelineEntryDto
{
    public string Kind { get; init; } = string.Empty;
    public DateTimeOffset At { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Detail { get; init; }
    public Guid? RefId { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public string? CreatorDisplayName { get; init; }
    public string? CreatorRoleLabel { get; init; }
    public int? Rating { get; init; }
    public LeadActivityType? ActivityType { get; init; }
}

public sealed class LeadJournalEntryDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string Action { get; init; } = string.Empty;
    public Guid ActorUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? MetadataJson { get; init; }

    /// <summary>Resolved from core Users + society role (French labels).</summary>
    public string? ActorDisplayName { get; init; }

    public string? ActorRoleLabel { get; init; }

    /// <summary>One-line French summary for admins (derived from action + metadata).</summary>
    public string? SummaryFr { get; init; }
}

/// <summary>Request to manually change a lead's status (also applies minimum score floor).</summary>
public sealed class ChangeLeadStatusRequest
{
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// Request to manually change a lead's temperature (Hot/High/Medium/Low/Cold).
/// The backend bumps LVI and SD to the minimum floor for the chosen temperature,
/// and records UpdatedById + UpdatedAt for audit tracking.
/// </summary>
public sealed class ChangeLeadTemperatureRequest
{
    public LeadTemperature Temperature { get; init; }
}

/// <summary>Request to add a manual tag to a lead.</summary>
public sealed class AddLeadTagRequest
{
    public string Tag { get; init; } = string.Empty;
}
