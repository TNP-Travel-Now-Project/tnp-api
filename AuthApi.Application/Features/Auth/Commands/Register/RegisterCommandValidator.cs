using FluentValidation;

namespace AuthApi.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(p => p.FullName)
                .NotEmpty().WithMessage("FullName is required")
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters");

            RuleFor(p => p.UserName)
                .NotEmpty().WithMessage("UserName is required")
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters");

            RuleFor(p => p.Email)
                .NotEmpty().WithMessage("Email is required")
                .MaximumLength(50).WithMessage("Email must not exceed 50 characters")
                .EmailAddress().WithMessage("Email is invalid");

            RuleFor(p => p.Age)
                .NotEmpty().WithMessage("Age is required")
                .GreaterThan(17).LessThan(26).WithMessage("Age must be between 18 and 25");

            RuleFor(p => p.PhoneNumber)
                .NotEmpty().WithMessage("PhoneNumber is required")
                .Length(10).WithMessage("Length number is not format")
                .Matches("[0-9]").WithMessage("Phone number have to format 0-9");

            RuleFor(x => x.Password)
               .NotEmpty().WithMessage("Password is not empty")
                .Length(8, 20)
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[~!@#$%^&*()_+=?]").WithMessage("Password must cotain at least one special character");
        }
    }
}
