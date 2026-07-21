namespace CrmPhotoVolta.Application.Crm.Commercials;

/// <summary>
/// Recomputes commercial KPI snapshots from CRM data (leads, quotes, calendar, activities).
/// </summary>
public interface ICommercialKpiSyncService
{
    /// <summary>Sync KPIs for the commercial profile linked to <paramref name="userId"/>.</summary>
    Task SyncForUserAsync(Guid societyId, Guid userId, CancellationToken ct = default);

    /// <summary>Sync KPIs for a commercial profile by id.</summary>
    Task SyncForProfileAsync(Guid societyId, Guid profileId, CancellationToken ct = default);
}
