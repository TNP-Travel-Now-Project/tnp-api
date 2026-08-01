namespace AuthApi.Application.Features.Auth.DTOs
{
    public sealed record RegisterResponse(
        Guid UserId,
        string FirstName,
        string LastName,
        string UserName,
        string Email,
        DateTime CreatedAt);
}
