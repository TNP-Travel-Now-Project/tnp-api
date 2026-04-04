using AuthApi.Application.Abstractions.Messaging;
using AuthApi.Domain.Enums;

namespace AuthApi.Application.Features.Users.Commands.CreateUser
{
    public record CreateUserCommand(
        Guid Id,
        int Age, 
        UserRole Role,
        string Name) : ICommand<Guid>;
}
