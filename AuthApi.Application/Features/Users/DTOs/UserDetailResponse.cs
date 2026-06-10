namespace AuthApi.Application.Features.Users.DTOs
{
    public sealed record UserDetailResponse
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = null!;
        public string UserName { get; init; } = null!;
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public DateOnly DateOfBirth { get; init; }
        public string? PhoneNumber { get; init; }
        public bool EmailConfirmed { get; init; }
        public bool IsLockedOut { get; init; }
        public bool LockoutEnabled { get; init; }
        public string[] Roles { get; init; } = [];
        public DateTimeOffset LockoutEnd { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}
