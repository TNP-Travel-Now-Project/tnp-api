using FluentValidation;
using System.Reflection.Metadata;

namespace AuthApi.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(p => p.FirstName)
                .NotEmpty().WithMessage("FullName is required")
                .MaximumLength(50).WithMessage("Full name must not longer than 100 characters");

            RuleFor(p => p.LastName)
                .NotEmpty().WithMessage("FullName is required")
                .MaximumLength(50).WithMessage("Full name must not longer than 100 characters");

            RuleFor(p => p.UserName)
                .NotEmpty().WithMessage("UserName is required")
                .MaximumLength(256).WithMessage("Full name must not longer than 100 characters");

            RuleFor(p => p.Email).EmailAddress().WithMessage("Email is invalid");

            RuleFor(p => p.PhoneNumber)
                .NotEmpty().WithMessage("PhoneNumber is required")
                .Length(10).WithMessage("Length number is not format")
                .Matches("[0-9]").WithMessage("Phone number have to format 0-9");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var minDob = today.AddYears(-25);
            var maxDob = today.AddYears(-18);

            RuleFor(p => p.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required")
                .InclusiveBetween(minDob, maxDob).WithMessage("You must be between 18 to 25 years old");

            RuleFor(x => x.Password)
               .NotEmpty().WithMessage("Password is not empty")
                .Length(8, 20)
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[~!@#$%^&*()_+=?]").WithMessage("Password must cotain at least one special character");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is not empty")
                .Equal(x => x.Password).WithMessage("Confirm password must match password");
        }
    }
}
