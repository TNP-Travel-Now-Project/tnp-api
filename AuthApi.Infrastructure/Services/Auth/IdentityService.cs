using AuthApi.Application.Abstractions.Repositories.Auth;
using AuthApi.Application.Abstractions.Repositories.Email;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.Commands.Login;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.DTOs.Login;
using AuthApi.Application.Features.Auth.DTOs.Register;
using AuthApi.Application.Features.Auth.DTOs.Token;
using AuthApi.Infrastructure.Common;
using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Services.Email;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System.Data;

namespace AuthApi.Infrastructure.Services.Auth
{
    public class IdentityService(
        ITokenService _tokenService,
        UserManager<ApplicationUser> _userManager,
        IEmailService _emailService,
        IBackgroundJobClient _jobClient,
        IOptions<AppSettings> _appSetting) : IIdentityService
    {
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
                FullName = user.FullName,
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

        public async Task<Result<RegisterResponse>> RegisterAsync(RegisterCommand req)
        {

            var user = new ApplicationUser(
                fullName: req.FullName,
                age: req.Age,
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

            var confirmLink =
            $"{_appSetting.Value.FrontendUrl}/api/auth/verify-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            _jobClient.Enqueue(() =>
            _emailService.SendEmailAsync(
                user.Email!,
                "Verify your email",
                $"Click to verify: <a href='{confirmLink}'>Verify Email</a>"));

            // Xoa user neu nhu chua xac minh
            _jobClient.Schedule<EmailCleanupJob>(p =>

                p.DeleteUnverifiedUser(user.Id),
                TimeSpan.FromHours(2)
            );

            return Result<RegisterResponse>.Success(
                new RegisterResponse(
                UserId: user.Id,
                FullName: user.FullName,
                Email: user.Email ?? string.Empty,
                CreateAt: user.CreatedAt)
            );
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
