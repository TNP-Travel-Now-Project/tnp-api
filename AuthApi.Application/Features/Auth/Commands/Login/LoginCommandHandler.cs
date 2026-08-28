using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;
using MediatR;

namespace AuthApi.Application.Features.Auth.Commands.Login
{
    public class LoginCommandhandler(IIdentityService _identities) : ICommandHandler<LoginCommand, Result<LoginResponse>>
    {
        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
            => await _identities.LoginAsync(request);
    }
}
