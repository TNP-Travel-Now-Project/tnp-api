namespace AuthApi.Application.Features.Auth.DTOs.Token
{
    public sealed record AuthResponse(
    string? AccessToken,
    string? RefreshToken,
    DateTime AccessTokenExpiresAt);
}
