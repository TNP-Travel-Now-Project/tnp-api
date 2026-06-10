using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.Logout;
using MediatR;

namespace AuthApi.Application.Features.Auth.Commands.Logout
{
    public class LogoutCommandHandler(IIdentityService _identities) : IRequestHandler<LogoutCommand, Result<LogoutResponse>>
    {
        public async Task<Result<LogoutResponse>> Handle(LogoutCommand request, CancellationToken cancellationToken)
                    => await _identities.LogoutAsync(request);
    }
}
