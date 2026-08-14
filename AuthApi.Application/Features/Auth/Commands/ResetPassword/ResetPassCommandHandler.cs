using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Domain.ObjectValues;
using MediatR;

namespace AuthApi.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPassCommandHandler(IIdentityService _identities) : IRequestHandler<ResetPasswordCommand, Result<NewPassResponse>>
    {
        public async Task<Result<NewPassResponse>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            if (!Email.TryCreate(request.Email, out var email))
                return Result<NewPassResponse>.Fail(AuthErrors.EmailInvalid);

            var newRequest = request with
            {
                Email = email!.Value,
                Otp = request.Otp
            };

            return await _identities.SetNewPassAsync(newRequest);
        }
    }
}
