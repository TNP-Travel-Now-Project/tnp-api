using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.Commands.Login;
using AuthApi.Application.Features.Auth.Commands.Logout;
using AuthApi.Application.Features.Auth.Commands.RefreshToken;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.Commands.ResetPassword;
using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Abstractions.Interfaces.Auth
{
    public interface IIdentityService
    {
        Task<Result<LoginResponse>> LoginAsync(LoginCommand request);
        Task<Result<LogoutResponse>> LogoutAsync(LogoutCommand request);
        Task<Result<RegisterResponse>> RegisterAsync(RegisterCommand request);
        Task<Result<bool>> VerifyEmailAsync(Guid userId, string token);
        Task<Result<OtpResponse>> SendOTPAsync(string email);
        Task<Result<NewPassResponse>> SetNewPassAsync(ResetPasswordCommand request);
        Task<Result<RefreshTokenResponse>> RefeshTokenAsync(RefreshTokenCommand request);
    }
}