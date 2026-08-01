namespace AuthApi.Application.Features.Auth.DTOs
{
    public record LoginResponse(
        string? accessToken,
        string? refreshToken,
        DateTime expired,
        Guid userId,
        string email,
        string[] roles);
}
