using AuthApi.Application.Abstractions.DTOs.Auth;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Abstractions.Interfaces.Auth;

public interface IIdentityService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> GoogleLoginAsync(string tokenId, CancellationToken cancellationToken = default);
    Task<Result<LogoutResponse>> LogoutAsync(CancellationToken cancellationToken = default);
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);
    Task<Result<OtpResponse>> SendOTPAsync(string email, CancellationToken cancellationToken = default);
    Task<Result<NewPassResponse>> SetNewPassAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result<RefreshTokenResponse>> RefreshTokenAsync(CancellationToken cancellationToken = default);
}