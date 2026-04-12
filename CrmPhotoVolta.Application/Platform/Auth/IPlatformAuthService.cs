using CrmPhotoVolta.Application.Platform.Auth.Dtos;

namespace CrmPhotoVolta.Application.Platform.Auth;

public interface IPlatformAuthService
{
    Task<PlatformLoginResult> LoginAsync(PlatformLoginRequest request, CancellationToken cancellationToken = default);
}
