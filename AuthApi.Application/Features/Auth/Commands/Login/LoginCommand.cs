using System.ComponentModel.DataAnnotations;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.Auth.Login;

namespace AuthApi.Application.Features.Auth.Commands.Login
{
    public sealed record LoginCommand(
        string Email,
        string Password,
        bool RememberMe
    ) : ICommand<Result<LoginResponse>>;
}