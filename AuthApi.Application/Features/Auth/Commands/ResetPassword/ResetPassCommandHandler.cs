using AuthApi.Application.Abstractions.DTOs.Auth;
using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Domain.ObjectValues;

namespace AuthApi.Application.Features.Auth.Commands.ResetPassword;

public class ResetPassCommandHandler(IIdentityService _services) : ICommandHandler<ResetPassCommand, Result<NewPassResponse>>
{
    public async Task<Result<NewPassResponse>> Handle(ResetPassCommand request, CancellationToken cancellationToken)
    {
        if (!Email.TryCreate(request.Email, out var email))
            return Result<NewPassResponse>.Fail(AuthErrors.EmailInvalid);

        var resetRequest = new ResetPasswordRequest(email!.Value, request.Otp, request.NewPass);
        return await _services.SetNewPassAsync(resetRequest, cancellationToken);
    }
}
