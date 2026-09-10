using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler(IIdentityService _services) : ICommandHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        => await _services.RefreshTokenAsync(cancellationToken);
}