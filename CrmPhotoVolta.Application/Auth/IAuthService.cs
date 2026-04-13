using CrmPhotoVolta.Application.Auth.Dtos;

namespace CrmPhotoVolta.Application.Auth;

public interface IAuthService
{
    Task<AuthTokensResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthTokensResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<MeResponse> GetMeAsync(Guid userId, Guid? societyId, CancellationToken cancellationToken = default);
}
