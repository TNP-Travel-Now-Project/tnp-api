using AuthApi.Application.Abstractions.Messaging.Command;

namespace AuthApi.Application.Features.Users.Commands.AssignRoles;

public sealed record AssignRolesCommand(
    Guid UserId,
    string[] Roles) : ICommand;
