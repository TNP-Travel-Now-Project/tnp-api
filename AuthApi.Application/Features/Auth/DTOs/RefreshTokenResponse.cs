namespace AuthApi.Application.Features.Auth.DTOs
{
    public sealed record RefreshTokenResponse(
        string accessToken,
        string? refreshtoken,
        DateTime expiredAt);
}
