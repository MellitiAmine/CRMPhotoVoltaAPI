namespace CrmPhotoVolta.Application.Crm.Commercials;

/// <summary>
/// Application-layer contract for commercial profile management.
/// Implementations live in CrmPhotoVolta.Infrastructure.
/// </summary>
public interface ICommercialService
{
    // ── Queries ───────────────────────────────────────────────────────────

    /// <summary>Returns a paged, filtered list of commercial profiles with KPIs and score.</summary>
    Task<(IReadOnlyList<CommercialListItemDto> Items, CommercialPageMeta Meta)>
        ListAsync(Guid societyId, Guid actorUserId, CommercialListQuery query, CancellationToken ct = default);

    /// <summary>Returns the full profile including attendance snapshot and emergency contact.</summary>
    Task<CommercialProfileDto> GetAsync(Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default);

    /// <summary>Returns aggregate stats for the KPI strip (totals, averages).</summary>
    Task<CommercialStatsDto> GetStatsAsync(Guid societyId, Guid actorUserId, CancellationToken ct = default);

    // ── Mutations ─────────────────────────────────────────────────────────

    /// <summary>Returns the profile linked to the authenticated user (commercial self-service).</summary>
    Task<CommercialProfileDto> GetMeAsync(Guid societyId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>Creates a new commercial profile (HR onboarding).</summary>
    Task<CreateCommercialResultDto> CreateAsync(Guid societyId, Guid actorId, CreateCommercialRequest request, CancellationToken ct = default);

    /// <summary>Updates HR fields (department, position, salary, status, etc.).</summary>
    Task<CommercialProfileDto> UpdateAsync(Guid societyId, Guid actorUserId, Guid id, UpdateCommercialRequest request, CancellationToken ct = default);

    /// <summary>
    /// Pushes a fresh KPI snapshot from an external source (e.g. a nightly job
    /// or the leads/calendar services) and recomputes the performance score.
    /// </summary>
    Task<CommercialProfileDto> UpdateKpisAndScoreAsync(Guid societyId, Guid actorUserId, Guid id, UpdateCommercialKpisRequest kpis, CancellationToken ct = default);

    /// <summary>Soft-deletes a commercial profile.</summary>
    Task DeleteAsync(Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default);

    /// <summary>Updates the current-month attendance snapshot and recomputes the score.</summary>
    Task<CommercialProfileDto> UpdateAttendanceAsync(
        Guid societyId, Guid actorUserId, Guid id, UpdateCommercialAttendanceRequest request, CancellationToken ct = default);

    /// <summary>Recomputes KPIs from CRM data and returns the refreshed profile.</summary>
    Task<CommercialProfileDto> SyncKpisAsync(Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default);

    /// <summary>Updates login email and/or password for the commercial's linked user account.</summary>
    Task UpdateAccountAsync(Guid societyId, Guid actorUserId, Guid id, UpdateCommercialAccountRequest request, CancellationToken ct = default);
}

/// <summary>Pagination metadata returned alongside list results.</summary>
public sealed record CommercialPageMeta(int Page, int PageSize, int TotalCount, int TotalPages);
