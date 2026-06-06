using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.Register;
using AuthApi.Domain.ObjectValues;

namespace AuthApi.Application.Features.Auth.Commands.Register
{
    public class SendOTPCommandHandler(IIdentityService _identities) : ICommandHandler<RegisterCommand, Result<RegisterResponse>>
    {
        public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var newEmail = Email.Create(request.Email);

            var newRes = request with
            {
                Email = newEmail.Value,
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = request.UserName,
                Password = request.Password,
                PhoneNumber = request.PhoneNumber
            };

            return await _identities.RegisterAsync(newRes);
        }
    }
}