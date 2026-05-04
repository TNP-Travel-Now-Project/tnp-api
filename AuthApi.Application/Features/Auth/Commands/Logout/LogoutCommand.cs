using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;

namespace AuthApi.Application.Features.Auth.Commands.Logout
{
    public sealed record LogoutCommand : ICommand<Result<bool>>;
}
