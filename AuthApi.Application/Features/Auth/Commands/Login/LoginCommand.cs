using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.Auth.Login;

namespace AuthApi.Application.Features.Auth.Commands.Login
{
    public sealed record LoginCommand : ICommand<Result<LoginResponse>>
    {
        public LoginCommand(string email, string password, bool rememberMe)
        {
            Email = email;
            Password = password;
            RememberMe = rememberMe;
        }

        public string Email { get; init; } = null!;
        public string Password { get; init; } = null!;
        public bool RememberMe { get; init; } = false;
    }
}
