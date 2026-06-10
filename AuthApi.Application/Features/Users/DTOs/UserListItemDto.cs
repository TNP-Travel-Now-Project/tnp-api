namespace AuthApi.Application.Features.Users.DTOs
{
    public sealed record UserListItemDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = null!;
        public string UserName { get; init; } = null!;
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public string[] Roles { get; init; } = [];
        public DateTime CreatedAt { get; init; }
    }
}
