using AuthApi.Domain.Enums;

namespace AuthApi.Application.Features.Auth.DTOs.Token
{
    public sealed record AuthUserDto
    {
        public Guid Id { get; init; }
        public UserRole Role { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
    }
}
