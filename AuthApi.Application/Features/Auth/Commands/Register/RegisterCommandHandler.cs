using AuthApi.Domain.Interfaces;
using AuthApi.Application.Abstractions.Repositories.Auth;
using AuthApi.Application.Features.Auth.DTOs.Register;
using AuthApi.Application.Common;
using AuthApi.Application.Abstractions.Messaging.Command;

namespace AuthApi.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandHandler(IIdentityService _identities) : ICommandHandler<RegisterCommand, Result<RegisterResponse>>
    {
        public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            return await _identities.RegisterAsync(request);
        }
    }
}
