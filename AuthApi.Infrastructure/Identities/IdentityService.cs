using AuthApi.Application.Abstractions.Repositories.Auth;
using AuthApi.Application.Abstractions.Repositories.Email;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.DTOs.Register;
using AuthApi.Infrastructure.Common;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

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
            $"{_appSetting.Value.FrontendUrl}/api/auth/verify-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            _jobClient.Enqueue(() => _emailService.SendEmailAsync(
                user.Email!,
                "Verify your email",
                $"Click to verify: <a href='{confirmLink}'>Verify Email</a>"));

            return Result<RegisterResponse>.Success(
                new RegisterResponse(
                UserId: user.Id,
                FullName: user.FullName,
                Email: user.Email ?? string.Empty,
                CreateAt: user.CreatedAt)
            );
        }

        public Task<Result<bool>> VerifyEmailAsync(RegisterCommand req)
        {
            throw new NotImplementedException();
        }
    }
}
