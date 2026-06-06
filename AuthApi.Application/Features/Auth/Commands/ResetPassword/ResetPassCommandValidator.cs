using AuthApi.Application.Common;
using FluentValidation;

namespace AuthApi.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPassCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPassCommandValidator()
        {
            RuleFor(p => p.Email)
                .NotEmpty().WithMessage(ValidationMessages.FieldRequired)
                .EmailAddress().WithMessage(ValidationMessages.EmailInvalid);

            RuleFor(p => p.Otp)
                .NotEmpty().WithMessage(ValidationMessages.OtpRequired)
                .Length(4).WithMessage(ValidationMessages.OtpLength)
                .Matches("^[0-9]+$").WithMessage(ValidationMessages.OtpDigits);

            RuleFor(p => p.NewPass)
                .NotEmpty().WithMessage(ValidationMessages.FieldRequired)
                .Length(8, 20).WithMessage(ValidationMessages.PasswordLengthRange)
                .Matches("[A-Z]").WithMessage(ValidationMessages.PasswordRequiresUppercase)
                .Matches("[a-z]").WithMessage(ValidationMessages.PasswordRequiresLowercase)
                .Matches("[0-9]").WithMessage(ValidationMessages.PasswordRequiresDigit)
                .Matches("[~!@#$%^&*()_+=?]").WithMessage(ValidationMessages.PasswordRequiresSpecial);
        }
    }
}
