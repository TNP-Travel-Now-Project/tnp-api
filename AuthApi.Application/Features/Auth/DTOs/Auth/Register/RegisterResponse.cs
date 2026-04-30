namespace AuthApi.Application.Features.Auth.DTOs.Auth.Register
{
    public sealed record RegisterResponse(
        Guid UserId,
        string FullName,
        string Email,
        DateTime CreatedAt);
}
