using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Application.Features.Auth.DTOs.Token;

namespace AuthApi.Application.Abstractions.Interfaces.Auth
{
    public interface ITokenService
    {
        Task<AuthResponse> GenerateTokensAsync(
            AuthUserDto user,
            IList<string> roles,
            int expiredDay = 15);
        Task<AuthResponse> RefreshTokenAsync();
        Task RevokeRefreshTokenAsync();
    }
}
