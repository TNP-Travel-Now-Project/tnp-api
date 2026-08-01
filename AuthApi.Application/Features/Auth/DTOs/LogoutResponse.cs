namespace AuthApi.Application.Features.Auth.DTOs
{
    public sealed record LogoutResponse(
        string Message,
        bool Success = true);
}
