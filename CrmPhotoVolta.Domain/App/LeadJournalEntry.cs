namespace CrmPhotoVolta.Domain.App;

/// <summary>
/// Append-only audit trail for a lead (assignments, status, calendar links, activity lifecycle, etc.).
/// Distinct from <see cref="LeadActivity"/> which records user-visible CRM work (calls, notes, signals).
/// </summary>
public sealed class LeadJournalEntry : SocietyScopedEntity
{
    public Guid LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    /// <summary>Stable action key, e.g. <c>commercial.assigned</c> (see API docs / client constants).</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Optional domain of the related row (e.g. <c>calendar_event</c>, <c>lead_activity</c>).</summary>
    public string? RelatedEntityType { get; set; }

    public Guid? RelatedEntityId { get; set; }

    /// <summary>JSON payload: before/after values, titles, free-form context.</summary>
    public string? MetadataJson { get; set; }
}
