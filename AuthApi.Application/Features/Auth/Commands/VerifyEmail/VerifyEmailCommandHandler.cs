using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;

namespace AuthApi.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandler(IIdentityService _services) : ICommandHandler<VerifyEmailCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        => await _services.VerifyEmailAsync(request.UserId, request.Token, cancellationToken);
}
