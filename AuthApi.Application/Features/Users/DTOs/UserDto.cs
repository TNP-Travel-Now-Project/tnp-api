using System.Data;

namespace AuthApi.Application.Features.Users.DTOs
{
    public sealed record UserDto
    {
        public Guid Id { get; init; }
        public int Age { get; init; }
        public string Role { get; init; } = null!;
        public string FullName { get; init; } = null!;
        public string Email{ get; init; } = null!;
        public DateTime CreatedAt{ get; init; }
    };
}
