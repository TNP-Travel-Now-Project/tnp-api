using FluentValidation;

namespace AuthApi.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(p => p.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

            RuleFor(p => p.UserName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

            RuleFor(p => p.Email)
                .NotEmpty().WithMessage("Email is required")
                .MaximumLength(50).WithMessage("Email must not exceed 50 characters.")
                .EmailAddress().WithMessage("Email is invalid");

            RuleFor(p => p.Age)
                .NotEmpty().WithMessage("Age is required.")
                .GreaterThan(17).LessThan(26).WithMessage("Age must be between 18 and 25.");

            RuleFor(x => x.password)
               .NotEmpty().WithMessage("Password is required")
               .MinimumLength(6).WithMessage("Password must be at least 6 characters");
        }
    }
}
