using AuthApi.Application.Abstractions.Messaging.Command;

namespace AuthApi.Application.Features.Users.Commands.LockoutUser;

public sealed record LockoutUserCommand(Guid UserId) : ICommand;
