using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.Register;

namespace AuthApi.Application.Features.Auth.Commands.Register
{
    public sealed record RegisterCommand(
        string Email,
        string FirstName,
        string LastName,
        string UserName,
        string PhoneNumber,
        DateOnly DateOfBirth,
        string Password,
        string ConfirmPassword) : ICommand<Result<RegisterResponse>>;
}
