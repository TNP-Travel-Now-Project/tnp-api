using AuthApi.Application.Features.Auth.DTOs.Auth;
using AuthApi.Application.Features.Auth.DTOs.Auth.Token;

namespace AuthApi.Application.Abstractions.Repositories.Auth
{
    public interface ITokenService
    {
        Task<AuthResponse> GenerateTokensAsync(
            AuthUserDto user, 
            IList<string> roles, 
            int expiredDay = 15);
        Task<AuthResponse> RefreshTokenAsync();
        void SetRefreshTokenCookie(string refreshToken, int days);
        Task RevokeRefreshTokenAsync();
        void ClearRefreshTokenCookie();
    }
}
