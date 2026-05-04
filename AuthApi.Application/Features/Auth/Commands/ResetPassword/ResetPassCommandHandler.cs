using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.Auth.ForgetPassword;
using AuthApi.Domain.ObjectValues;
using MediatR;

namespace AuthApi.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPassCommandHandler(IIdentityService _identities) : IRequestHandler<ResetPasswordCommand, Result<NewPassResponse>>
    {
        public async Task<Result<NewPassResponse>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var email = Email.Create(request.Email);

            var newRequest = request with
            {
                Email = email.Value,
                Otp = request.Otp
            };

            return await _identities.SetNewPassAsync(newRequest);
        }
    }
}
