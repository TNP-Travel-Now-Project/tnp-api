namespace AuthApi.Application.Features.Auth.DTOs
{
    public sealed record NewPassResponse
    {
        public string Email { get; init; } = null!;
        public string NewPassword { get; init; } = null!;
    };
}
