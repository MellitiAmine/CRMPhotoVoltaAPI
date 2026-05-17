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

    /// <summary>Creates a new commercial profile (HR onboarding).</summary>
    Task<CommercialProfileDto> CreateAsync(Guid societyId, Guid actorId, CreateCommercialRequest request, CancellationToken ct = default);

    /// <summary>Updates HR fields (department, position, salary, status, etc.).</summary>
    Task<CommercialProfileDto> UpdateAsync(Guid societyId, Guid actorUserId, Guid id, UpdateCommercialRequest request, CancellationToken ct = default);

    /// <summary>
    /// Pushes a fresh KPI snapshot from an external source (e.g. a nightly job
    /// or the leads/calendar services) and recomputes the performance score.
    /// </summary>
    Task<CommercialProfileDto> UpdateKpisAndScoreAsync(Guid societyId, Guid actorUserId, Guid id, UpdateCommercialKpisRequest kpis, CancellationToken ct = default);

    /// <summary>Soft-deletes a commercial profile.</summary>
    Task DeleteAsync(Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default);
}

/// <summary>Pagination metadata returned alongside list results.</summary>
public sealed record CommercialPageMeta(int Page, int PageSize, int TotalCount, int TotalPages);
