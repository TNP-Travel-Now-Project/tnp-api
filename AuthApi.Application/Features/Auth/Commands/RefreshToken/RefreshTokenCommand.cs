using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Features.Auth.Commands.RefreshToken
{
    public sealed record RefreshTokenCommand : ICommand<Result<RefreshTokenResponse>>;
}