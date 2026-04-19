using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Abstractions.Repositories.Auth;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs.Auth.Register;
using AuthApi.Domain.ObjectValues;

namespace AuthApi.Application.Features.Auth.Commands.Register
{
    public class SendOTPCommandHandler(IIdentityService _identities) : ICommandHandler<RegisterCommand, Result<RegisterResponse>>
    {
        public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var newAge = AgeUser.Create(request.Age);
            var newEmail = Email.Create(request.Email);

            var newRes = request with
            {
                Age = newAge,
                Email = newEmail.Value,
                FullName = request.FullName,
                Password = request.Password,
                PhoneNumber = request.PhoneNumber,
                UserName = request.UserName
            };

            return await _identities.RegisterAsync(newRes);
        }
    }
}