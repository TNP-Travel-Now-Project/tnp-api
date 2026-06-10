using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.ForgetPassword;

namespace AuthApi.Application.Features.Auth.Commands.ResetPassword
{
    public sealed record ResetPasswordCommand(
        string Email,
        string Otp,
        string NewPass) : ICommand<Result<NewPassResponse>>;
}
