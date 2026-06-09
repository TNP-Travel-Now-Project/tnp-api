using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;

namespace AuthApi.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(IUserWriteRepository _userRepo)
    : ICommandHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var requestDto = new DTOs.UpdateUserRequest
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth
        };

        var updated = await _userRepo.UpdateUserAsync(request.UserId, requestDto, cancellationToken);
        if (!updated)
            throw new NotFoundException($"User with ID {request.UserId} not found");
    }
}
