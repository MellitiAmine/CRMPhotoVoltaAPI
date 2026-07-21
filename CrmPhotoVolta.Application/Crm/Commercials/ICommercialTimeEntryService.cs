namespace CrmPhotoVolta.Application.Crm.Commercials;

public interface ICommercialTimeEntryService
{
    Task<CommercialTimeMonthBundleDto> GetMonthAsync(
        Guid societyId,
        Guid actorUserId,
        Guid commercialProfileId,
        int year,
        int month,
        CancellationToken ct = default);

    Task<CommercialTimeEntryDto> CreateAsync(
        Guid societyId,
        Guid actorUserId,
        Guid commercialProfileId,
        CreateCommercialTimeEntryRequest request,
        CancellationToken ct = default);

    Task<CommercialTimeEntryDto> UpdateAsync(
        Guid societyId,
        Guid actorUserId,
        Guid commercialProfileId,
        Guid entryId,
        UpdateCommercialTimeEntryRequest request,
        CancellationToken ct = default);

    Task DeleteAsync(
        Guid societyId,
        Guid actorUserId,
        Guid commercialProfileId,
        Guid entryId,
        CancellationToken ct = default);

    Task<CommercialAttendanceMonthDto> RecomputeMonthAsync(
        Guid societyId,
        Guid actorUserId,
        Guid commercialProfileId,
        int year,
        int month,
        CancellationToken ct = default);

    Task<IReadOnlyList<CommercialAttendanceMonthDto>> ListMonthsAsync(
        Guid societyId,
        Guid actorUserId,
        Guid commercialProfileId,
        int? year,
        CancellationToken ct = default);
}
