namespace AuthApi.Application.Features.Users.DTOs
{
    public sealed record roleRowsDto
    {
        public Guid UserId { get; init; }
        public string Role { get; init; } = null!;
    }
}
