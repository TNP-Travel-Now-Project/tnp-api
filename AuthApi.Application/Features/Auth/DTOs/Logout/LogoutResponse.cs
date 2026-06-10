namespace AuthApi.Application.Features.Auth.DTOs.Logout
{
    public sealed record LogoutResponse(
        string Message,
        bool Success = true);
}
