using AuthApi.Domain.Interfaces;
using AuthApi.Application.Abstractions.Messaging;
using AuthApi.Application.Abstractions.Repositories.Auth;
using AuthApi.Application.Features.Auth.DTOs.Register;
using AuthApi.Application.Common;

namespace AuthApi.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandHandler(IIdentityService _auth) : ICommandHandler<RegisterCommand, Result<RegisterResponse>>
    {
        public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            return await _auth.RegisterAsync(request);
        }
    }
}
