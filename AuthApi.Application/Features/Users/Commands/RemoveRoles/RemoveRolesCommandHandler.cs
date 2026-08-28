using AuthApi.Application.Abstractions.Interfaces.Repositories.User;
using AuthApi.Application.Abstractions.Messaging.Command;

namespace AuthApi.Application.Features.Users.Commands.RemoveRoles;

public sealed class RemoveRolesCommandHandler(IUserWriteRepository _userRepo)
    : ICommandHandler<RemoveRolesCommand>
{
    public async Task Handle(RemoveRolesCommand request, CancellationToken cancellationToken)
    {
        await _userRepo.RemoveRolesAsync(request.UserId, request.Roles, cancellationToken);
    }
}
