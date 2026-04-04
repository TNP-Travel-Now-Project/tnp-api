using AuthApi.Domain.Enums;

namespace AuthApi.Application.Features.Auth.DTOs
{
    public class AuthUserDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public UserRole Role { get; init; }
    }
}
