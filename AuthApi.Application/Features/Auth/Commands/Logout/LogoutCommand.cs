using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.Auth.Logout;

namespace AuthApi.Application.Features.Auth.Commands.Logout
{
    public sealed record LogoutCommand : ICommand<Result<LogoutResponse>>;
}
