using AuthApi.Application.Abstractions.Messaging;
using AuthApi.Domain.Enums;

namespace AuthApi.Application.Features.Auth.Commands.CreateUser
{
    public record LoginCommand(
        Guid Id,
        int Age, 
        UserRole Role,
        string Name) : ICommand<Guid>;
}
