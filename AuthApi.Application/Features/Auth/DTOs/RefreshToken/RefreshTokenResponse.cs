namespace AuthApi.Application.Features.Auth.DTOs.RefreshToken
{
    public sealed record RefreshTokenResponse(
        string accessToken,
        string? refreshtoken,
        DateTime expiredAt);
}
