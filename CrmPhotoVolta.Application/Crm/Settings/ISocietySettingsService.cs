namespace CrmPhotoVolta.Application.Crm.Settings;

public interface ISocietySettingsService
{
    Task<SocietySettingsDto> GetAsync(Guid societyId, CancellationToken cancellationToken = default);
    Task<SocietySettingsDto> UpdateAsync(Guid societyId, UpdateSocietySettingsRequest request, CancellationToken cancellationToken = default);
}

public sealed class SocietySettingsDto
{
    public string DataJson { get; init; } = "{}";
}

public sealed class UpdateSocietySettingsRequest
{
    public string DataJson { get; init; } = "{}";
}
