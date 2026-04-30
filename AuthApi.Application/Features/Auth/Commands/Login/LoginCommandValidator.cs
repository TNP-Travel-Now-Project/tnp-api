using FluentValidation;

namespace AuthApi.Application.Features.Auth.Commands.Login
{
    public class ResetPassCommandValidator : AbstractValidator<LoginCommand>
    {
        public ResetPassCommandValidator()
        {
            RuleFor(p => p.Email)
                .NotEmpty().WithMessage("Email is not empty")
                .EmailAddress().WithMessage("Email wrong format");

            RuleFor(p => p.Password)
                .NotEmpty().WithMessage("Password is not empty")
                .Length(8, 20).WithMessage("Password must be between 8 and 20 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[~!@#$%^&*()_+=?]").WithMessage("Password must cotain at least one special character");
        }
    }
}
