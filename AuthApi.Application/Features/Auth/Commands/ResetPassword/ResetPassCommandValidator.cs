using AuthApi.Application.Common;
using AuthApi.Application.Common.Validation;
using AuthApi.Domain.ObjectValues;
using FluentValidation;

namespace AuthApi.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPassCommandValidator : AbstractValidator<ResetPassCommand>
    {
        public ResetPassCommandValidator()
        {
            RuleFor(p => p.Email)
                .NotEmpty().WithMessage(ValidationMessages.FieldRequired)
                .Must(Email.IsValid).WithMessage(ValidationMessages.EmailInvalid);

            RuleFor(p => p.Otp)
                .NotEmpty().WithMessage(ValidationMessages.OtpRequired)
                .Length(4).WithMessage(ValidationMessages.OtpLength)
                .Matches("^[0-9]+$").WithMessage(ValidationMessages.OtpDigits);

            RuleFor(p => p.NewPass).StrongPassword();
        }
    }
}
