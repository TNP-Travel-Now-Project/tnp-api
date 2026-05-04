using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Interfaces.Email;
using AuthApi.Application.Abstractions.Repositories.Email;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.Commands.Login;
using AuthApi.Application.Features.Auth.Commands.RefreshToken;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.Commands.ResetPassword;
using AuthApi.Application.Features.Auth.DTOs.Auth;
using AuthApi.Application.Features.Auth.DTOs.Auth.ForgetPassword;
using AuthApi.Application.Features.Auth.DTOs.Auth.Login;
using AuthApi.Application.Features.Auth.DTOs.Auth.RefreshToken;
using AuthApi.Application.Features.Auth.DTOs.Auth.Register;
using AuthApi.Infrastructure.Common;
using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Services.Email;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Data;
using System.Security.Cryptography;

namespace AuthApi.Infrastructure.Services.Auth
{
    public class IdentityService(
        ITokenService _tokenService,
        UserManager<ApplicationUser> _userManager,
        IEmailChecker _emailChecker,
        IEmailService _emailService,
        IBackgroundJobClient _jobClient,
        IOptions<AppSettings> _appSetting,
        IConnectionMultiplexer _redis) : IIdentityService
    {
        public string OTPKey { get => "otp:"; }

        public async Task<Result<LoginResponse>> LoginAsync(LoginCommand req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return Result<LoginResponse>.Fail("Invalid credentials");

            if (await _userManager.IsLockedOutAsync(user))
                return Result<LoginResponse>.Fail("User is locked");

            if (!user.EmailConfirmed)
                return Result<LoginResponse>.Fail("Email is not confirm");

            if (!await _userManager.CheckPasswordAsync(user, req.Password))
            {
                await _userManager.AccessFailedAsync(user);
                return Result<LoginResponse>.Fail("Invalid credentials");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            string roleName = string.Join(", ", roles);

            var userDto = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName!,
                Role = roleName
            };

            var token = await _tokenService.GenerateTokensAsync(userDto, roles);

            return Result<LoginResponse>.Success(
                new LoginResponse(
                    accessToken: token.AccessToken,
                    refreshToken: null,
                    expired: token.AccessTokenExpiresAt,
                    userId: user.Id,
                    email: user.Email!,
                    role: roleName)
            );
        }

        public async Task<Result<RefreshTokenResponse>> RefeshTokenAsync(RefreshTokenCommand refresh)
        {
            var token = await _tokenService.RefreshTokenAsync();

            return token.AccessToken != null
                ? Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(
                        accessToken: token.AccessToken,
                        refreshtoken: null,
                        expiredAt: token.AccessTokenExpiresAt))

                : Result<RefreshTokenResponse>.Fail("Occured error while RefreshToken handle");
        }

        public async Task<Result<RegisterResponse>> RegisterAsync(RegisterCommand req)
        {
            var db = _redis.GetDatabase();

            var user = new ApplicationUser(
                firstName: req.FirstName,
                lastName: req.LastName,
                dateOfBirth: req.DateOfBirth,
                email: req.Email,
                userName: req.UserName);

            var result = await _userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return Result<RegisterResponse>.Fail(string.Join(", ", errors));
            }

            await _userManager.AddToRoleAsync(user, "User");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            if (string.IsNullOrEmpty(token))
                return Result<RegisterResponse>.Fail("Failed to generate email confirmation token");

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

        public async Task<Result<OtpResponse>> SendOTPAsync(string email)
        {
            var confirm = await _userManager.FindByEmailAsync(email);
            if (confirm == null)
                return Result<OtpResponse>.Fail($"This email address {email} is not contain in the system");

            var checkEmail = await _emailChecker.IsValidAsync(email);
            if (!checkEmail)
                return Result<OtpResponse>.Fail($"Email Invalid");

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
                return Result<OtpResponse>.Fail($"Redis Invalid");

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
                return Result<NewPassResponse>.Fail("OTP expired or not found");

            if (keyExist != reset.Otp)
                return Result<NewPassResponse>.Fail("OTP incorrect");

            await db.KeyDeleteAsync(key);

            var user = await _userManager.FindByEmailAsync(reset.Email);
            if (user == null)
                return Result<NewPassResponse>.Fail("User not found");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var newPass = await _userManager.ResetPasswordAsync(user, token, reset.NewPass);

            if (!newPass.Succeeded)
            {
                var errors = newPass?.Errors.Select(e => e.Description).ToList() ?? new List<string>();
                return Result<NewPassResponse>.Fail(string.Join(", ", errors));
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
                return Result<bool>.Fail("User not found");

            if (string.IsNullOrEmpty(token))
                return Result<bool>.Fail("Token is invalid");

            var confirmEmail = await _userManager.ConfirmEmailAsync(user, token);

            if (!confirmEmail.Succeeded)
            {
                var errors = confirmEmail.Errors.Select(e => e.Description).ToList();
                return Result<bool>.Fail(string.Join(", ", errors));
            }

            return Result<bool>.Success(true);
        }
    }
}
