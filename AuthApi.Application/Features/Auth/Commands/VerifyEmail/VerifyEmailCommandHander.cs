using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;

namespace AuthApi.Application.Features.Auth.Commands.VerifyEmail
{
    public class VerifyEmailCommandHander(IIdentityService _identities) : ICommandHandler<VerifyEmailCommand, Result<bool>>
    {
        public async Task<Result<bool>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
            return await _identities.VerifyEmailAsync(request.UserId, request.Token);
        }
    }
}
