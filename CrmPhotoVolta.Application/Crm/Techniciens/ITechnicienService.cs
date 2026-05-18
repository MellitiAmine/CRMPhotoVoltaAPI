namespace CrmPhotoVolta.Application.Crm.Techniciens;

/// <summary>
/// Application-layer contract for technicien profile management.
/// Implementations live in CrmPhotoVolta.Infrastructure.
/// </summary>
public interface ITechnicienService
{
    Task<(IReadOnlyList<TechnicienListItemDto> Items, TechnicienPageMeta Meta)>
        ListAsync(Guid societyId, Guid actorUserId, TechnicienListQuery query, CancellationToken ct = default);

    Task<TechnicienProfileDto> GetAsync(Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default);

    Task<TechnicienStatsDto> GetStatsAsync(Guid societyId, Guid actorUserId, CancellationToken ct = default);

    Task<TechnicienProfileDto> CreateAsync(Guid societyId, Guid actorId, CreateTechnicienRequest request, CancellationToken ct = default);

    Task<TechnicienProfileDto> UpdateAsync(Guid societyId, Guid actorUserId, Guid id, UpdateTechnicienRequest request, CancellationToken ct = default);

    Task<TechnicienProfileDto> UpdateKpisAndScoreAsync(Guid societyId, Guid actorUserId, Guid id, UpdateTechnicienKpisRequest kpis, CancellationToken ct = default);

    Task DeleteAsync(Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default);
}

public sealed record TechnicienPageMeta(int Page, int PageSize, int TotalCount, int TotalPages);
