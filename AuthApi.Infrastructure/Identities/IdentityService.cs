using AuthApi.Application.Abstractions.Repositories.Auth;
using AuthApi.Application.Abstractions.Repositories.Email;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.DTOs.Register;
using AuthApi.Application.Features.Users.DTOs;
using AuthApi.Infrastructure.Common;
using AuthApi.Infrastructure.Services.Email;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.Options;
using System.Security.Principal;

namespace AuthApi.Infrastructure.Identities
{
    public class IdentityService(
        ITokenService _tokenService,
        UserManager<ApplicationUser> _userManager,
        IEmailService _emailService,
        IBackgroundJobClient _jobClient,
        IOptions<AppSettings> _appSetting) : IIdentityService
    {
        public async Task<Result<RegisterResponse>> RegisterAsync(RegisterCommand req)
        {
            var user = new ApplicationUser(
                fullName: req.FullName,
                age: req.Age,
                email: req.Email,
                userName: req.UserName);

            var result = await _userManager.CreateAsync(user, req.password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return Result<RegisterResponse>.Fail(string.Join(", ", errors));
            }

            await _userManager.AddToRoleAsync(user, "User");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var confirmLink =
            $"{_appSetting.Value.FrontendUrl}/api/auths/verify-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            _jobClient.Enqueue(() => _emailService.SendEmailAsync(
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
