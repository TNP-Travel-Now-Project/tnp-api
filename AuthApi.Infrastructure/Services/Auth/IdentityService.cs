using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Repositories.Email;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.Commands.Login;
using AuthApi.Application.Features.Auth.Commands.Logout;
using AuthApi.Application.Features.Auth.Commands.RefreshToken;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.Commands.ResetPassword;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Domain.Entities.Financial;
using AuthApi.Infrastructure.Common;
using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Services.Email;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Data;
using System.Security.Cryptography;

namespace AuthApi.Infrastructure.Services.Auth
{
    public class IdentityService(
        ITokenService _tokenService,
        UserManager<ApplicationUser> _userManager,
        IEmailService _emailService,
        IBackgroundJobClient _jobClient,
        IOptions<AppSettings> _appSetting,
        IConnectionMultiplexer _redis,
        IAuthCookieService _tokenHandler,
        ILogger<IdentityService> _logger,
        AppDbContext _dbContext) : IIdentityService
    {
        public string OTPKey { get => "otp:"; }

        public async Task<Result<LoginResponse>> LoginAsync(LoginCommand req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return Result<LoginResponse>.Fail(AuthErrors.InvalidCredentials);

            if (await _userManager.IsLockedOutAsync(user))
                return Result<LoginResponse>.Fail(AuthErrors.UserLockedOut);

            if (!user.EmailConfirmed)
                return Result<LoginResponse>.Fail(AuthErrors.EmailNotConfirmed);

            if (!await _userManager.CheckPasswordAsync(user, req.Password))
            {
                await _userManager.AccessFailedAsync(user);
                return Result<LoginResponse>.Fail(AuthErrors.InvalidCredentials);
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName!,
                Roles = [.. roles]
            };

            var token = await _tokenService.GenerateTokenServiceAsync(userDto, roles);

            return Result<LoginResponse>.Success(
                new LoginResponse(
                    accessToken: token.AccessToken,
                    refreshToken: null,
                    expired: token.AccessTokenExpiresAt,
                    userId: user.Id,
                    email: user.Email!,
                    roles: [.. roles])
            );
        }

        public async Task<Result<LogoutResponse>> LogoutAsync(LogoutCommand request)
        {
            await _tokenService.RevokeRefreshTokenServiceAsync();
            _tokenHandler.ClearTokenCookies();

            return Result<LogoutResponse>.Success(
                new LogoutResponse(Message: "Logout successful"));
        }

        public async Task<Result<RefreshTokenResponse>> RefeshTokenAsync(RefreshTokenCommand refresh)
        {
            var token = await _tokenService.RefreshTokenServiceAsync();

            if (token.Value == null || token.Value.AccessToken == null)
                return Result<RefreshTokenResponse>.Fail(
                    new Error(
                        ErrorCodes.TokenRefreshError,
                        "An error occurred while processing the refresh token"));

            return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(
                        accessToken: token.Value.AccessToken,
                        refreshtoken: null,
                        expiredAt: token.Value.AccessTokenExpiresAt));
        }

        public async Task<Result<RegisterResponse>> RegisterAsync(RegisterCommand req)
        {
            //var db = _redis.GetDatabase();

            var user = new ApplicationUser(
                userName: req.UserName,
                firstName: req.FirstName,
                lastName: req.LastName,
                dateOfBirth: req.DateOfBirth,
                email: req.Email);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var result = await _userManager.CreateAsync(user, req.Password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return Result<RegisterResponse>.Fail(
                        new Error(ErrorCodes.RegistrationError, string.Join(", ", errors)));
                }

                await _userManager.AddToRoleAsync(user, "User");

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                if (string.IsNullOrEmpty(token))
                {
                    await transaction.RollbackAsync();
                    return Result<RegisterResponse>.Fail(AuthErrors.TokenGenerationFailed);
                }

                await transaction.CommitAsync();

                var confirmLink =
                $"{_appSetting.Value.FrontendUrl}/api/auth/verify-email" +
                                                    $"?userId={user.Id}" +
                                                    $"&token={Uri.EscapeDataString(token)}";

                _jobClient.Enqueue(() =>
                    _emailService.SendEmailAsync(
                        user.Email!,
                        "Verify your email",
                        $"Click to verify: <a href='{confirmLink}'>Verify Email</a>")
                );

                // Xoa user neu nhu chua xac minh
                _jobClient.Schedule<EmailCleanupJob>(p =>
                    p.DeleteUnverifiedUser(user.Id),
                    TimeSpan.FromHours(2)
                );

                return Result<RegisterResponse>.Success(
                    new RegisterResponse(
                    UserId: user.Id,
                    FirstName: user.FirstName,
                    LastName: user.LastName,
                    UserName: user.UserName!,
                    Email: user.Email ?? string.Empty,
                    CreatedAt: user.CreatedAt)
                );
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(); throw;
            }
        }

        public async Task<Result<OtpResponse>> SendOTPAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("SendOTP requested for unknown email {Email}", email);
                return Result<OtpResponse>.Success(new OtpResponse
                {
                    Email = email,
                    Expired = DateTime.UtcNow.AddMinutes(5)
                });
            }

            var key = $"{OTPKey}{email.ToLower()}";
            var db = _redis.GetDatabase();
            var expired = TimeSpan.FromMinutes(5);
            var otp = RandomNumberGenerator.GetInt32(1000, 9999).ToString();

            _jobClient.Enqueue(() =>
                _emailService.SendEmailAsync(
                    email,
                    "Confirm Code OTP for your account",
                    $"Verification Code: {otp}"
                )
            );

            var isRedis = await db.StringSetAsync(key, otp, expired);
            if (!isRedis)
                return Result<OtpResponse>.Fail(AuthErrors.RedisFailure);

            return Result<OtpResponse>.Success(new OtpResponse
            {
                Email = email,
                Expired = DateTime.UtcNow.AddMinutes(5)
            });
        }

        public async Task<Result<NewPassResponse>> SetNewPassAsync(ResetPasswordCommand reset)
        {
            var key = $"{OTPKey}{reset.Email.ToLower()}";
            var db = _redis.GetDatabase();
            var keyExist = await db.StringGetAsync(key);

            if (keyExist.IsNullOrEmpty)
                return Result<NewPassResponse>.Fail(AuthErrors.OtpExpired);

            if (keyExist != reset.Otp)
                return Result<NewPassResponse>.Fail(AuthErrors.OtpIncorrect);

            await db.KeyDeleteAsync(key);

            var user = await _userManager.FindByEmailAsync(reset.Email);
            if (user == null)
                return Result<NewPassResponse>.Fail(AuthErrors.UserNotFound);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var newPass = await _userManager.ResetPasswordAsync(user, token, reset.NewPass);

            if (!newPass.Succeeded)
            {
                var errors = newPass?.Errors.Select(e => e.Description).ToList() ?? new List<string>();
                return Result<NewPassResponse>.Fail(
                    new Error(ErrorCodes.ValidationError, string.Join(", ", errors)));
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Result<NewPassResponse>.Success(new NewPassResponse
            {
                Email = reset.Email
            });
        }

        public async Task<Result<bool>> VerifyEmailAsync(Guid userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return Result<bool>.Fail(AuthErrors.UserNotFound);

            if (string.IsNullOrEmpty(token))
                return Result<bool>.Fail(AuthErrors.TokenInvalid);

            var confirmEmail = await _userManager.ConfirmEmailAsync(user, token);

            if (!confirmEmail.Succeeded)
            {
                var errors = confirmEmail.Errors.Select(e => e.Description).ToList();
                return Result<bool>.Fail(
                    new Error(ErrorCodes.ValidationError, string.Join(", ", errors)));
            }

            return Result<bool>.Success(true);
        }
    }
}
