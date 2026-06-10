using AuthApi.Application.Abstractions.Messaging.Command;

namespace AuthApi.Application.Features.Users.Commands.RemoveRoles;

public sealed record RemoveRolesCommand(
    Guid UserId,
    string[] Roles) : ICommand;
