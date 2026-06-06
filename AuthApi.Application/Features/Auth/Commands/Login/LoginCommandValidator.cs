using AuthApi.Application.Common;
using FluentValidation;

namespace AuthApi.Application.Features.Auth.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(p => p.Email)
                .NotEmpty().WithMessage(ValidationMessages.FieldRequired)
                .EmailAddress().WithMessage(ValidationMessages.EmailInvalid);

            RuleFor(p => p.Password)
                .NotEmpty().WithMessage(ValidationMessages.FieldRequired)
                .Length(8, 20).WithMessage(ValidationMessages.PasswordLengthRange)
                .Matches("[A-Z]").WithMessage(ValidationMessages.PasswordRequiresUppercase)
                .Matches("[a-z]").WithMessage(ValidationMessages.PasswordRequiresLowercase)
                .Matches("[0-9]").WithMessage(ValidationMessages.PasswordRequiresDigit)
                .Matches("[~!@#$%^&*()_+=?]").WithMessage(ValidationMessages.PasswordRequiresSpecial);
        }
    }
}
