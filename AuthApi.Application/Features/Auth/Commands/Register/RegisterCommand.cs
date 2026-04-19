using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.Register;

namespace AuthApi.Application.Features.Auth.Commands.Register
{
    public sealed record RegisterCommand(
        int Age,            
        string Email,
        string FullName,
        string UserName,
        string PhoneNumber,
        string Password) : ICommand<Result<RegisterResponse>>;
}
