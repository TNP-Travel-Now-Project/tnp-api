using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;
using MediatR;

namespace AuthApi.Application.Features.Auth.Commands.RefreshToken
{
    public class RefeshTokenCommandHandler(IIdentityService _identities) : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
    {
        public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            return await _identities.RefeshTokenAsync(request);
        }
    }
}