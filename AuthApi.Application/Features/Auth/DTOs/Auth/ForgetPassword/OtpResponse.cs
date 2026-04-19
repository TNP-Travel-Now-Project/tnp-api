namespace AuthApi.Application.Features.Auth.DTOs.Auth.ForgetPassword
{
    public sealed record OtpResponse
    {
        public string Email { get; init; } = null!;
        public DateTime Expired { get; init; }
    }
}
