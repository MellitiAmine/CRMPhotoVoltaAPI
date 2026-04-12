using CrmPhotoVolta.Application.Societies.Dtos;

namespace CrmPhotoVolta.Application.Platform.Societies;

public interface IPlatformSocietyService
{
    Task<IReadOnlyList<SocietyDto>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<SocietyDto> GetAsync(Guid societyId, CancellationToken cancellationToken = default);
    Task<SocietyDto> CreateAsync(CreatePlatformSocietyRequest request, CancellationToken cancellationToken = default);
    Task<SocietyDto> UpdateAsync(Guid societyId, UpdatePlatformSocietyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid societyId, CancellationToken cancellationToken = default);
}
