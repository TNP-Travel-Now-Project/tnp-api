using AuthApi.Application.Abstractions.Interfaces.Repositories.User;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;

namespace AuthApi.Application.Features.Users.Commands.LockoutUser;

public sealed class LockoutUserCommandHandler(IUserWriteRepository _userRepo)
    : ICommandHandler<LockoutUserCommand>
{
    public async Task Handle(LockoutUserCommand request, CancellationToken cancellationToken)
    {
        var result = await _userRepo.SoftDeleteUserAsync(request.UserId, cancellationToken);
        if (!result)
            throw new NotFoundException($"User with ID {request.UserId} not found");
    }
}
