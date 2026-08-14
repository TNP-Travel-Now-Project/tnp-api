using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Domain.ObjectValues;

namespace AuthApi.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandHandler(IIdentityService _identities) : ICommandHandler<RegisterCommand, Result<RegisterResponse>>
    {
        public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (!Email.TryCreate(request.Email, out var newEmail))
                return Result<RegisterResponse>.Fail(AuthErrors.EmailInvalid);

            var newRes = request with { Email = newEmail!.Value };

            return await _identities.RegisterAsync(newRes);
        }
    }
}