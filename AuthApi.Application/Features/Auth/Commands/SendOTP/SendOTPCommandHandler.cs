using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.Auth.ForgetPassword;
using AuthApi.Domain.ObjectValues;

namespace AuthApi.Application.Features.Auth.Commands.SendOTP
{
    public class SendOTPCommandHandler(IIdentityService _identities) : ICommandHandler<SendOTPCommand, Result<OtpResponse>>
    {
        public async Task<Result<OtpResponse>> Handle(SendOTPCommand request, CancellationToken cancellationToken)
        {
            var email = Email.Create(request.Email);
            return await _identities.SendOTPAsync(email.Value);
        }
    }
}