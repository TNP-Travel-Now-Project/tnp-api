using AuthApi.Domain.Interfaces;

namespace AuthApi.Application.Features.Auth.DTOs.Auth.Login
{
    public record LoginResponse(
        string accessToken,
        string? refreshToken,
        DateTime expired,
        Guid userId,
        string email,
        string role);
}
