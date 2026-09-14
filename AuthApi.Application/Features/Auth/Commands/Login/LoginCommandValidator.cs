using AuthApi.Application.Common;
using AuthApi.Application.Common.Validation;
using AuthApi.Domain.ObjectValues;
using FluentValidation;

namespace AuthApi.Application.Features.Auth.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(p => p.Email)
                .NotEmpty().WithMessage(ValidationMessages.FieldRequired)
                .Must(Email.IsValid).WithMessage(ValidationMessages.EmailInvalid);

            RuleFor(x => x.Password).StrongPassword();
        }
    }
}
