using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Abstractions.Messaging.Command;

namespace AuthApi.Application.Features.Users.Commands.AssignRoles;

public sealed class AssignRolesCommandHandler(IUserWriteRepository _userRepo)
    : ICommandHandler<AssignRolesCommand>
{
    public async Task Handle(AssignRolesCommand request, CancellationToken cancellationToken)
    {
        await _userRepo.AssignRolesAsync(request.UserId, request.Roles, cancellationToken);
    }
}
