using AuthApi.Domain.Enums;

namespace AuthApi.Application.Features.Auth.DTOs
{
    public sealed record AuthUserDto
    {
        public Guid Id { get; init; }
        public string? Role { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public string UserName { get; init; } = null!;
    }
}
