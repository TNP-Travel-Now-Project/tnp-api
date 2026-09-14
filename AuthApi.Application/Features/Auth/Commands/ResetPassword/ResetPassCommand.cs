using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Features.Auth.Commands.ResetPassword
{
    public sealed record ResetPassCommand(
        string Email,
        string Otp,
        string NewPass) : ICommand<Result<NewPassResponse>>;
}
