using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Abstractions.Repositories.Auth;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.Auth.Login;

namespace AuthApi.Application.Features.Auth.Commands.Login
{
    public class LoginCommandhandler(IIdentityService _identities) : ICommandHandler<LoginCommand, Result<LoginResponse>>
    {
        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            return await _identities.LoginAsync(request);
        }
    }
}
