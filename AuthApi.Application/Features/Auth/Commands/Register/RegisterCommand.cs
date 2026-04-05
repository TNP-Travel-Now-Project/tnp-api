using AuthApi.Application.Abstractions.Messaging;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.Register;
using AuthApi.Domain.Enums;

namespace AuthApi.Application.Features.Auth.Commands.Register
{
    public sealed record RegisterCommand(
        int Age,            
        string Email,
        string FullName,
        string UserName,
        string password) : ICommand<Result<RegisterResponse>>;
}
