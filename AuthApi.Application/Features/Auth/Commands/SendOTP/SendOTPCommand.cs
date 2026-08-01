using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Features.Auth.Commands.SendOTP
{
    public sealed record SendOTPCommand(string Email) : ICommand<Result<OtpResponse>>;
}
