namespace AuthApi.Application.Features.Auth.DTOs.Auth.Logout
{
    public sealed record LogoutResponse(
        string Message,
        bool Success = true);
}
