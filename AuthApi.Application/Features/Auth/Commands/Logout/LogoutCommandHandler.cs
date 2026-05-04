using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Common;
using MediatR;

namespace AuthApi.Application.Features.Auth.Commands.Logout
{
    public class LogoutCommandHandler(IIdentityService _identities) : IRequestHandler<LogoutCommand, Result<bool>>
    {
        public Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken) => _identities.LogoutAsync(request);
    }
}
