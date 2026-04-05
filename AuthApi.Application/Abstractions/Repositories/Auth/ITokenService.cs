using AuthApi.Application.Features.Auth.DTOs.Token;

namespace AuthApi.Application.Abstractions.Repositories.Auth
{
    public interface ITokenService
    {
        Task<AuthResponse> GenerateTokensAsync(AuthUserDto user);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}
