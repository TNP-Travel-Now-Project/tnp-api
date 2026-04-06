using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;

namespace AuthApi.Application.Features.Auth.Commands.VerifyEmail
{
    public sealed record VerifyEmailCommand : ICommand<Result<bool>>
    {
        public VerifyEmailCommand(Guid userId, string token)
        {
            UserId = userId;
            Token = token;
        }

        public Guid UserId { get; init; }
        public string Token { get; init; } = null!;
    }
}
