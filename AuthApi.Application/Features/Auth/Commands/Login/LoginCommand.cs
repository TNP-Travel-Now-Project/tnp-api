using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Domain.Enums;

namespace AuthApi.Application.Features.Auth.Commands.CreateUser
{
    public record LoginCommand(
        Guid Id,
        int Age, 
        UserRole Role,
        string Name) : ICommand<Guid>;
}
