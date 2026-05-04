using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.Commands.Login;
using AuthApi.Application.Features.Auth.Commands.RefreshToken;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.Commands.ResetPassword;
using AuthApi.Application.Features.Auth.DTOs.Auth.ForgetPassword;
using AuthApi.Application.Features.Auth.DTOs.Auth.Login;
using AuthApi.Application.Features.Auth.DTOs.Auth.RefreshToken;
using AuthApi.Application.Features.Auth.DTOs.Auth.Register;

namespace AuthApi.Application.Abstractions.Interfaces.Auth
{
    public interface IIdentityService
    {
        Task<Result<LoginResponse>> LoginAsync(LoginCommand req);
        Task<Result<RegisterResponse>> RegisterAsync(RegisterCommand req);
        Task<Result<bool>> VerifyEmailAsync(Guid userId, string token);
        Task<Result<OtpResponse>> SendOTPAsync(string email);
        Task<Result<NewPassResponse>> SetNewPassAsync(ResetPasswordCommand reset);
        Task<Result<RefreshTokenResponse>> RefeshTokenAsync(RefreshTokenCommand refresh);
    }
}