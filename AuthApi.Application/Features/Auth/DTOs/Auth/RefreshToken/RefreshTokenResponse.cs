namespace AuthApi.Application.Features.Auth.DTOs.Auth.RefreshToken
{
    public sealed record RefreshTokenResponse(
        string accessToken,
        string? refreshtoken, 
        DateTime expiredAt);
}
