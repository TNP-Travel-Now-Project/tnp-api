namespace AuthApi.Application.Features.Auth.DTOs.Register
{
    public sealed record RegisterResponse(
        Guid UserId,
        string FullName,
        string Email,
        DateTime CreateAt);
}
