namespace AuthApi.Application.Features.Auth.DTOs.Auth.Token
{
    public sealed record AuthResponse(
    string AccessToken,
    string? RefreshToken,
    DateTime AccessTokenExpiresAt);
}
