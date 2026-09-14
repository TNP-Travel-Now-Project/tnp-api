using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Features.Auth.Commands.GoogleLogin;

public class GoogleLoginCommandHandler(IIdentityService _services) : ICommandHandler<GoogleLoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.TokenId))
            return Result<LoginResponse>.Fail(new Error(ErrorCodes.InvalidToken, "TokenId is required"));

        return await _services.GoogleLoginAsync(request.TokenId, cancellationToken);
    }
}
