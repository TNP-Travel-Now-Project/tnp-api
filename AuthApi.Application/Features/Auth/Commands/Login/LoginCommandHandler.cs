using AuthApi.Application.Abstractions.DTOs.Auth;
using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler(IIdentityService _services) : ICommandHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var loginRequest = new LoginRequest(request.Email, request.Password);
        return await _services.LoginAsync(loginRequest, cancellationToken);
    }
}
