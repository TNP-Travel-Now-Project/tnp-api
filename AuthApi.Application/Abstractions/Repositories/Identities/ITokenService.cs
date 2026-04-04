using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Abstractions.Repositories.Identities
{
    public interface ITokenService
    {
        Task<AuthResponse> GenerateTokensAsync(AuthUserDto user);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}
