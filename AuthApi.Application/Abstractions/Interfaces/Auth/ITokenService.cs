using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Abstractions.Interfaces.Auth
{
    public interface ITokenService
    {
        Task<AuthResponse> GenerateTokenServiceAsync(
            AuthUserDto user,
            IList<string> roles,
            int expiredDay = 15);
        Task<AuthResponse> RefreshTokenServiceAsync();
        Task RevokeRefreshTokenServiceAsync();
    }
}
