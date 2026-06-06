namespace AuthApi.Application.Features.Auth.DTOs.ForgetPassword
{
    public sealed record OtpResponse
    {
        public string Email { get; init; } = null!;
        public DateTime Expired { get; init; }
    }
}
