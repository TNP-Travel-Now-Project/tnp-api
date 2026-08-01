namespace AuthApi.Application.Features.Auth.DTOs
{
    public sealed record OtpResponse
    {
        public string Email { get; init; } = null!;
        public DateTime Expired { get; init; }
    }
}
