using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Societies.Dtos;

namespace CrmPhotoVolta.Application.Societies;

public interface ISocietyService
{
    Task<IReadOnlyList<SocietyDto>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SocietyDto> GetAsync(Guid userId, Guid societyId, CancellationToken cancellationToken = default);
    Task<SocietyDto> CreateAsync(Guid userId, CreateSocietyRequest request, CancellationToken cancellationToken = default);
    Task<SocietyDto> UpdateAsync(Guid userId, Guid societyId, UpdateSocietyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid societyId, CancellationToken cancellationToken = default);
}
