using AuthApi.Application.Common;
using AuthApi.Application.Common.Validation;
using FluentValidation;

namespace AuthApi.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(p => p.FirstName)
                .NotEmpty().WithMessage(ValidationMessages.FullNameRequired)
                .MaximumLength(50).WithMessage(ValidationMessages.FullNameMaxLength);

            RuleFor(p => p.LastName)
                .NotEmpty().WithMessage(ValidationMessages.FullNameRequired)
                .MaximumLength(50).WithMessage(ValidationMessages.FullNameMaxLength);

            RuleFor(p => p.UserName)
                .NotEmpty().WithMessage(ValidationMessages.UserNameRequired)
                .MaximumLength(256).WithMessage(ValidationMessages.UserNameMaxLength);

            RuleFor(p => p.Email)
                .NotEmpty().WithMessage(ValidationMessages.FieldRequired)
                .EmailAddress().WithMessage(ValidationMessages.EmailInvalid);

            RuleFor(p => p.PhoneNumber)
                .NotEmpty().WithMessage(ValidationMessages.PhoneNumberRequired)
                .Length(10).WithMessage(ValidationMessages.PhoneNumberLength)
                .Matches("^[0-9]+$").WithMessage(ValidationMessages.PhoneNumberDigits);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var minDob = today.AddYears(-25);
            var maxDob = today.AddYears(-18);

            RuleFor(p => p.DateOfBirth)
                .NotEmpty().WithMessage(ValidationMessages.DateOfBirthRequired)
                .InclusiveBetween(minDob, maxDob).WithMessage(ValidationMessages.DateOfBirthRange);

            RuleFor(x => x.Password).StrongPassword();

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage(ValidationMessages.FieldRequired)
                .Equal(x => x.Password).WithMessage(ValidationMessages.PasswordsMustMatch);
        }
    }
}
